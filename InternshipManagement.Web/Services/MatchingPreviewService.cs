using System.Data;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services.Matching;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Web.Services;

public interface IMatchingPreviewService
{
    Task<MatchingPreview?> PreviewAsync(ulong campaignId, int? seed = null);
    Task<(bool Ok, string? Error)> ConfirmAsync(ulong campaignId, int seed, string fingerprint);
}

public class MatchingPreviewService(ApplicationDbContext db) : IMatchingPreviewService
{
    public async Task<MatchingPreview?> PreviewAsync(ulong campaignId, int? seed = null)
    {
        // One consistent read snapshot, including when students edit preferences concurrently.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        var input = await LoadAsync(campaignId, seed);
        await tx.CommitAsync();
        return input is null ? null : MatchingPreviewPlanner.Build(input);
    }

    public async Task<(bool Ok, string? Error)> ConfirmAsync(ulong campaignId, int seed, string fingerprint)
    {
        // Serialize confirmations and coordinate with the existing manual assignment topic locks.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var campaign = await db.Campaigns.FromSqlInterpolated(
            $"SELECT * FROM campaigns WHERE id = {campaignId} FOR UPDATE").AsNoTracking().ToListAsync();
        if (campaign.Count == 0) return (false, "Không tìm thấy đợt ghép.");
        await db.Topics.FromSqlInterpolated(
            $"SELECT * FROM topics WHERE campaign_id = {campaignId} ORDER BY id FOR UPDATE").AsNoTracking().ToListAsync();

        var input = (await LoadAsync(campaignId, seed))!;
        if (!string.Equals(input.Fingerprint, fingerprint, StringComparison.Ordinal))
            return (false, "Dữ liệu đã thay đổi. Vui lòng xem lại kết quả trước khi xác nhận.");
        var preview = MatchingPreviewPlanner.Build(input);
        if (!preview.CanConfirm)
            return (false, preview.Errors.FirstOrDefault() ?? "Không có thay đổi cần xác nhận hoặc đợt đã lưu trữ.");

        var existing = await db.TopicStudentAssignments.Where(a => a.CampaignId == campaignId).ToDictionaryAsync(a => a.StudentUserId);
        foreach (var row in preview.Rows.Where(r => !r.Existing))
        {
            if (!existing.TryGetValue(row.Student.Id, out var assignment))
            {
                assignment = new TopicStudentAssignment { CampaignId = campaignId, StudentUserId = row.Student.Id };
                db.TopicStudentAssignments.Add(assignment);
            }
            else if (assignment.TopicId == row.TopicId && assignment.Status == row.Status) continue;
            assignment.TopicId = row.TopicId;
            assignment.Status = row.Status;
            assignment.AssignedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return (true, null);
    }

    private async Task<MatchingInput?> LoadAsync(ulong campaignId, int? seed)
    {
        var campaign = await db.Campaigns.AsNoTracking().SingleOrDefaultAsync(c => c.Id == campaignId);
        if (campaign is null) return null;
        var studentRoleId = await db.Roles.Where(r => r.Name == AppRoles.Student).Select(r => r.Id).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Không tìm thấy vai trò sinh viên.");
        var students = await db.Users.AsNoTracking().Where(u => u.MajorId == campaign.MajorId &&
                db.UserRoles.Any(r => r.UserId == u.Id && r.RoleId == studentRoleId))
            .OrderBy(u => u.Id).Select(u => new MatchingStudent(u.Id, u.FullName, u.UserName ?? u.Id)).ToListAsync();
        var topics = await db.Topics.AsNoTracking().Where(t => t.CampaignId == campaignId).OrderBy(t => t.Id)
            .Select(t => new MatchingTopic(t.Id, t.Title, t.MaxStudents, t.ApprovalStatus)).ToListAsync();
        var preferences = await db.StudentTopicPreferences.AsNoTracking()
            .Where(p => p.Topic != null && p.Topic.CampaignId == campaignId)
            .OrderBy(p => p.StudentId).ThenBy(p => p.Priority).ThenBy(p => p.TopicId)
            .Select(p => new MatchingPreference(p.StudentId, p.TopicId, p.Priority, p.CreatedAt)).ToListAsync();
        var assignments = await db.TopicStudentAssignments.AsNoTracking().Where(a => a.CampaignId == campaignId)
            .OrderBy(a => a.StudentUserId).ThenBy(a => a.Id)
            .Select(a => new MatchingAssignment(a.StudentUserId, a.TopicId, a.Status)).ToListAsync();
        return new MatchingInput(campaignId, campaign.Name, campaign.Status, campaign.MatchingOrder,
            campaign.NoPreferencePolicy, campaign.RandomSeed ?? seed ?? Random.Shared.Next(), students, topics, preferences, assignments);
    }
}
