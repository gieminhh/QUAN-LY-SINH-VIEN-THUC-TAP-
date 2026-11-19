using System.Data;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.ViewModels;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace InternshipManagement.Web.Services;

public sealed class TopicPreferenceService(ApplicationDbContext db, ILogger<TopicPreferenceService> logger)
{
    private const string StaleMessage = "Nguyện vọng đã thay đổi ở tab khác. Vui lòng tải lại trang trước khi lưu.";

    public async Task<TopicPreferencePage?> GetPageAsync(string userId, ulong? campaignId)
    {
        var student = await GetStudentAsync(userId);
        if (student == null) return null;

        var available = ForStudent(student)
            .Where(c => c.Status == CampaignStatuses.Open || Lists(student).Any(p => p.CampaignId == c.Id));
        var campaigns = await available.OrderByDescending(c => c.Status == CampaignStatuses.Open)
            .ThenByDescending(c => c.StartDate).ThenBy(c => c.Id).ToListAsync();
        var campaign = campaignId.HasValue
            ? campaigns.Find(c => c.Id == campaignId.Value)
            : campaigns.FirstOrDefault();
        if (campaign == null) return campaignId.HasValue ? null : new TopicPreferencePage();

        var saved = await Lists(student).Include(p => p.Choices).ThenInclude(p => p.Topic)
            .SingleOrDefaultAsync(p => p.CampaignId == campaign.Id);
        var choices = saved?.Choices.OrderBy(p => p.PreferenceRank).ToList() ?? [];
        var window = await GetWindowAsync(campaign.Id);
        return new TopicPreferencePage
        {
            Campaigns = campaigns,
            Input = new TopicPreferenceInput
            {
                CampaignId = campaign.Id,
                ResearchDirection = saved?.ResearchDirection,
                Version = saved?.Version,
                TopicIds = choices.Select(p => (ulong?)p.TopicId).ToList()
            },
            Topics = await ApprovedTopics(campaign.Id).OrderBy(t => t.Title).ThenBy(t => t.Id).ToListAsync(),
            SavedChoices = choices,
            SavedAtUtc = saved?.SavedAt,
            ClosesAt = window?.ClosesAt,
            LockedReason = LockedReason(campaign, window)
        };
    }

    public async Task<string?> SaveAsync(string userId, TopicPreferenceInput input)
    {
        var direction = input.ResearchDirection?.Trim();
        var topicIds = input.TopicIds.Where(id => id.HasValue).Select(id => id.GetValueOrDefault()).ToArray();
        if (direction?.Length > 180) return "Hướng quan tâm tối đa 180 ký tự.";
        if (topicIds.Distinct().Count() != topicIds.Length) return "Mỗi đề tài chỉ được chọn một lần.";

        try
        {
            // Serializable reads keep the checked registration window and topic statuses valid
            // until the ordered list is committed.
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var student = await GetStudentAsync(userId);
            if (student == null) return "Bạn cần hồ sơ sinh viên để đăng ký nguyện vọng.";
            var campaign = await ForStudent(student).SingleOrDefaultAsync(c => c.Id == input.CampaignId);
            if (campaign == null) return "Đợt đăng ký không thuộc ngành của bạn.";
            var window = await GetWindowAsync(campaign.Id);
            var lockedReason = LockedReason(campaign, window);
            if (lockedReason != null) return lockedReason;

            if (topicIds.Length > 0 &&
                await ApprovedTopics(campaign.Id).CountAsync(t => topicIds.Contains(t.Id)) != topicIds.Length)
                return "Chỉ chọn đề tài đã được phê duyệt trong đợt đăng ký này.";

            var list = await db.TopicPreferenceLists
                .SingleOrDefaultAsync(p => p.CampaignId == campaign.Id && p.StudentProfileId == student.Id);
            if (list?.Version != input.Version) return StaleMessage;

            if (list == null)
            {
                list = new TopicPreferenceList { CampaignId = campaign.Id, StudentProfileId = student.Id };
                db.TopicPreferenceLists.Add(list);
            }
            else
            {
                // Clear the old ranks inside the transaction before inserting the new order.
                await db.TopicPreferences.Where(p => p.CampaignId == campaign.Id && p.StudentProfileId == student.Id)
                    .ExecuteDeleteAsync();
            }

            list.ResearchDirection = string.IsNullOrEmpty(direction) ? null : direction;
            list.Version = Guid.NewGuid().ToString("N");
            list.SavedAt = DateTime.UtcNow;
            db.TopicPreferences.AddRange(topicIds.Select((id, index) => new TopicPreference
            {
                CampaignId = campaign.Id,
                StudentProfileId = student.Id,
                PreferenceRank = (uint)(index + 1),
                TopicId = id
            }));
            await db.SaveChangesAsync();

            lockedReason = LockedReason(campaign, window);
            if (lockedReason != null) return lockedReason;
            await transaction.CommitAsync();
            return null;
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1062 or 1205 or 1213 })
        {
            return StaleMessage;
        }
        catch (MySqlException ex) when (ex.Number is 1205 or 1213)
        {
            return StaleMessage;
        }
        catch (Exception ex) when (ex is DbUpdateException or MySqlException)
        {
            logger.LogError(ex, "Could not save topic preferences for campaign {CampaignId}", input.CampaignId);
            return "Không thể lưu nguyện vọng. Vui lòng tải lại trang để kiểm tra trước khi thử lại.";
        }
    }

    private Task<StudentProfile?> GetStudentAsync(string userId) =>
        db.StudentProfiles.AsNoTracking().SingleOrDefaultAsync(s => s.UserId == userId && s.User!.IsActive);

    private IQueryable<Campaign> ForStudent(StudentProfile student) =>
        db.Campaigns.AsNoTracking().Where(c => c.MajorId == student.MajorId);

    private IQueryable<TopicPreferenceList> Lists(StudentProfile student) =>
        db.TopicPreferenceLists.AsNoTracking().Where(p => p.StudentProfileId == student.Id);

    private IQueryable<Topic> ApprovedTopics(ulong campaignId) =>
        db.Topics.AsNoTracking().Where(t => t.CampaignId == campaignId && t.Status == TopicStatuses.Approved);

    private Task<CampaignWindow?> GetWindowAsync(ulong campaignId) =>
        db.CampaignWindows.AsNoTracking().SingleOrDefaultAsync(w => w.CampaignId == campaignId && w.Stage == "REGISTRATION");

    private static string? LockedReason(Campaign campaign, CampaignWindow? window)
    {
        if (campaign.Status != CampaignStatuses.Open) return "Đợt đăng ký đã đóng.";
        if (window == null) return "Đợt chưa có lịch đăng ký nguyện vọng.";
        var now = CampaignRulesService.Now;
        if (now < window.OpensAt) return "Chưa đến thời gian đăng ký nguyện vọng.";
        if (now > window.ClosesAt) return "Đã hết hạn đăng ký. Nguyện vọng đã được khóa.";
        return null;
    }
}
