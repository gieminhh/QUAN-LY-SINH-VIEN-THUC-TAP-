using System.Data;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace InternshipManagement.Web.Services;

public sealed class ParticipationService(ApplicationDbContext db, IUserScopeService scopes)
{
    public static DateTime VietnamNow => DateTime.UtcNow.AddHours(7);
    public Task<bool> HasRole(string user, string role) => db.Users.AnyAsync(u => u.Id == user && u.IsActive &&
        db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == role)));
    public async Task<bool> IsStudent(string user, Campaign campaign) =>
        await HasRole(user, AppRoles.Student) && await db.Users.AnyAsync(u => u.Id == user && u.MajorId == campaign.MajorId);
    public async Task<bool> OwnsTopic(string lecturer, Topic topic) => await HasRole(lecturer, AppRoles.Lecturer) &&
        (topic.CreatedByUserId == lecturer || await db.TopicLecturers.AnyAsync(t => t.TopicId == topic.Id && t.LecturerUserId == lecturer));
    public IQueryable<ApplicationUser> Candidates(Topic topic) => db.Users.AsNoTracking().Where(u => u.IsActive && db.Campaigns.Any(c => c.Id == topic.CampaignId && c.MajorId == u.MajorId) &&
        db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == AppRoles.Student)) &&
        db.StudentTopicPreferences.Any(p => p.StudentId == u.Id && p.TopicId == topic.Id));
    public async Task<string?> Window(Campaign campaign, bool ranking)
    {
        var policy = await db.Set<ParticipationPolicy>().SingleOrDefaultAsync(p => p.CampaignId == campaign.Id);
        if (campaign.Status != CampaignStatuses.Open) return "Đợt đã đóng.";
        if (policy == null) return "Đợt chưa được cấu hình đăng ký.";
        if (ranking && !policy.LecturerRankingEnabled) return "Đợt chưa bật danh sách ưu tiên giảng viên.";
        var open = ranking ? policy.RankingOpensAt : policy.RegistrationOpensAt;
        var close = ranking ? policy.RankingClosesAt : policy.RegistrationClosesAt;
        if (VietnamNow < open) return "Chưa đến thời gian mở danh sách.";
        if (VietnamNow > close) return "Đã quá hạn. Danh sách chỉ được xem.";
        return null;
    }
    private async Task<Campaign?> Lock(ulong id) => await db.Campaigns
        .FromSqlInterpolated($"SELECT * FROM campaigns WHERE id={id} FOR UPDATE").SingleOrDefaultAsync();
    private async Task<string?> Change(ulong campaignId, Func<Campaign, Task<string?>> action, bool? rankingWindow = false)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var campaign = await Lock(campaignId);
        if (campaign == null) return "Không tìm thấy đợt.";
        var error = await action(campaign);
        if (error != null) return error;
        await db.SaveChangesAsync();
        if (rankingWindow.HasValue) {
            error = await Window(campaign, rankingWindow.Value);
            if (error != null) return error;
        }
        await tx.CommitAsync();
        return null;
    }
    public Task<string?> SavePolicy(string actor, ParticipationPolicy input) => Change(input.CampaignId, async campaign => {
        if (!await HasManagerRole(actor) || !await scopes.CanManageCampaignAsync(actor, campaign.Id)) return "Bạn không có quyền quản lý đợt.";
        if (input.GroupsAllowed && campaign.Kind != CampaignKinds.Project) return "Thực tập chỉ đăng ký cá nhân.";
        if (input.MaxGroupSize is < 2 or > 20) return "Quy mô nhóm từ 2 đến 20 thành viên.";
        if (input.RegistrationClosesAt < input.RegistrationOpensAt ||
            input.RegistrationOpensAt < campaign.StartDate || input.RegistrationClosesAt >= campaign.EndDate.Date.AddDays(1))
            return "Lịch đăng ký phải đúng thứ tự và nằm trong đợt.";
        if (!input.LecturerRankingEnabled) { input.RankingOpensAt = input.RegistrationOpensAt; input.RankingClosesAt = input.RegistrationClosesAt; }
        if (input.LecturerRankingEnabled && (input.RankingClosesAt < input.RankingOpensAt ||
            input.RankingOpensAt < campaign.StartDate || input.RankingClosesAt >= campaign.EndDate.Date.AddDays(1)))
            return "Lịch xếp hạng phải đúng thứ tự và nằm trong đợt.";
        var largest = await db.Set<CampaignParticipant>().Where(p => p.CampaignId == campaign.Id && p.GroupId != null)
            .GroupBy(p => p.GroupId).Select(g => g.Count()).ToListAsync();
        if (largest.Count > 0 && (!input.GroupsAllowed || largest.Max() > input.MaxGroupSize))
            return "Còn nhóm đang đăng ký; không thể tắt nhóm hoặc giảm dưới số thành viên hiện có.";
        var current = await db.Set<ParticipationPolicy>().FindAsync(campaign.Id);
        if (current == null) db.Add(input); else db.Entry(current).CurrentValues.SetValues(input);
        return null;
    }, null);
    public async Task<bool> HasManagerRole(string actor) => await HasRole(actor, AppRoles.Admin) ||
        await HasRole(actor, AppRoles.TrainingManager) || await HasRole(actor, AppRoles.FacultyManager);
    public Task<string?> RegisterIndividual(string student, ulong campaignId) => Change(campaignId, async campaign => {
        if (!await IsStudent(student, campaign)) return "Sinh viên không thuộc phạm vi đợt.";
        var error = await Window(campaign, false); if (error != null) return error;
        if (await db.Set<CampaignParticipant>().AnyAsync(p => p.CampaignId == campaignId && p.StudentId == student)) return "Bạn đã đăng ký trong đợt. Hãy rời đăng ký cũ trước.";
        db.Add(new CampaignParticipant { CampaignId = campaignId, StudentId = student }); return null;
    });
    public Task<string?> CreateGroup(string leader, ulong campaignId, string name) => Change(campaignId, async campaign => {
        if (!await IsStudent(leader, campaign)) return "Sinh viên không thuộc phạm vi đợt.";
        var error = await Window(campaign, false); if (error != null) return error;
        var policy = await db.Set<ParticipationPolicy>().FindAsync(campaignId);
        if (campaign.Kind != CampaignKinds.Project || policy?.GroupsAllowed != true) return "Đợt này chỉ đăng ký cá nhân.";
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100) return "Tên nhóm từ 1 đến 100 ký tự.";
        if (await db.Set<CampaignParticipant>().AnyAsync(p => p.CampaignId == campaignId && p.StudentId == leader)) return "Bạn đã đăng ký trong đợt. Hãy rời đăng ký cũ trước.";
        var group = new ProjectRegistrationGroup { CampaignId = campaignId, LeaderId = leader, Name = name.Trim() };
        db.Add(group); await db.SaveChangesAsync();
        db.Add(new CampaignParticipant { CampaignId = campaignId, StudentId = leader, GroupId = group.Id }); return null;
    });
    public async Task<string?> Invite(string actor, ulong groupId, string student)
    {
        var group = await db.Set<ProjectRegistrationGroup>().AsNoTracking().SingleOrDefaultAsync(g => g.Id == groupId);
        if (group == null) return "Không tìm thấy nhóm.";
        return await Change(group.CampaignId, async campaign => {
            group = await db.Set<ProjectRegistrationGroup>().AsNoTracking().SingleOrDefaultAsync(g => g.Id == groupId);
            if (group == null) return "Nhóm đã được giải tán.";
            if (group.LeaderId != actor || !await IsStudent(actor, campaign)) return "Chỉ nhóm trưởng được mời.";
            var error = await Window(campaign, false); if (error != null) return error;
            if (!await IsStudent(student, campaign) || student == actor) return "Thành viên phải là sinh viên khác cùng ngành.";
            if (await db.Set<CampaignParticipant>().AnyAsync(p => p.CampaignId == campaign.Id && p.StudentId == student)) return "Sinh viên đã có đăng ký trong đợt.";
            var existing = await db.Set<ProjectGroupInvitation>().FindAsync(groupId, student);
            if (existing != null && existing.Status != "DECLINED") return "Đã có lời mời cho sinh viên này.";
            if (existing == null) db.Add(new ProjectGroupInvitation { GroupId = groupId, StudentId = student });
            else { existing.Status = "PENDING"; existing.InvitedAt = DateTime.UtcNow; existing.RespondedAt = null; }
            return null;
        });
    }
    public async Task<string?> Respond(string student, ulong groupId, bool accept)
    {
        var group = await db.Set<ProjectRegistrationGroup>().AsNoTracking().SingleOrDefaultAsync(g => g.Id == groupId);
        if (group == null) return "Không tìm thấy nhóm.";
        return await Change(group.CampaignId, async campaign => {
            group = await db.Set<ProjectRegistrationGroup>().AsNoTracking().SingleOrDefaultAsync(g => g.Id == groupId);
            if (group == null) return "Nhóm đã được giải tán.";
            if (!await IsStudent(student, campaign)) return "Sinh viên không thuộc phạm vi đợt.";
            var error = await Window(campaign, false); if (error != null) return error;
            var invitation = await db.Set<ProjectGroupInvitation>().FindAsync(groupId, student);
            if (invitation?.Status != "PENDING") return "Lời mời không còn chờ xác nhận.";
            if (accept) {
                var policy = await db.Set<ParticipationPolicy>().FindAsync(campaign.Id);
                if (campaign.Kind != CampaignKinds.Project || policy?.GroupsAllowed != true) return "Đợt này chỉ đăng ký cá nhân.";
                if (await db.Set<CampaignParticipant>().AnyAsync(p => p.CampaignId == campaign.Id && p.StudentId == student)) return "Bạn đã tham gia một đăng ký khác trong đợt.";
                if (await db.Set<CampaignParticipant>().CountAsync(p => p.GroupId == groupId) >= policy.MaxGroupSize) return "Nhóm đã đủ thành viên.";
                db.Add(new CampaignParticipant { CampaignId = campaign.Id, StudentId = student, GroupId = groupId });
            }
            invitation.Status = accept ? "ACCEPTED" : "DECLINED"; invitation.RespondedAt = DateTime.UtcNow; return null;
        });
    }
    public Task<string?> Leave(string student, ulong campaignId) => Change(campaignId, async campaign => {
        if (!await IsStudent(student, campaign)) return "Sinh viên không thuộc phạm vi đợt.";
        var error = await Window(campaign, false); if (error != null) return error;
        var participant = await db.Set<CampaignParticipant>().FindAsync(campaignId, student);
        if (participant == null) return "Bạn chưa đăng ký.";
        if (participant.GroupId is { } groupId) {
            var group = await db.Set<ProjectRegistrationGroup>().FindAsync(groupId);
            if (group?.LeaderId == student) {
                await db.Set<CampaignParticipant>().Where(p => p.GroupId == groupId).ExecuteDeleteAsync();
                db.Entry(participant).State = EntityState.Detached;
                await db.Set<ProjectRegistrationGroup>().Where(g => g.Id == groupId).ExecuteDeleteAsync();
            } else {
                db.Remove(participant);
                // A student may be invited again after leaving.
                var invitation = await db.Set<ProjectGroupInvitation>().FindAsync(groupId, student);
                if (invitation != null) db.Remove(invitation);
            }
        } else db.Remove(participant);
        return null;
    });
    public Task<string?> SaveRanking(string lecturer, ulong topicId, List<string> students, string? version, string reason) => RankChange(lecturer, topicId, students, version, reason);
    private async Task<string?> RankChange(string lecturer, ulong topicId, List<string> students, string? version, string reason)
    {
        var topic = await db.Topics.AsNoTracking().SingleOrDefaultAsync(t => t.Id == topicId);
        if (topic == null) return "Không tìm thấy đề tài.";
        return await Change(topic.CampaignId, async campaign => {
            // Refresh topic after acquiring the campaign lock; never trust submitted candidate IDs.
            topic = await db.Topics.AsNoTracking().SingleAsync(t => t.Id == topicId);
            if (!await OwnsTopic(lecturer, topic)) return "Bạn không phụ trách đề tài này.";
            var error = await Window(campaign, true); if (error != null) return error;
            if (topic.ApprovalStatus != TopicApprovalStatuses.Approved || topic.Status != TopicStatuses.Published) return "Chỉ xếp hạng đề tài đã duyệt và công khai.";
            var ids = students.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (ids.Distinct().Count() != ids.Count) return "Không xếp hạng trùng sinh viên.";
            if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000) return "Nhập tiêu chí/lý do ưu tiên từ 1 đến 1000 ký tự.";
            if (await Candidates(topic).CountAsync(u => ids.Contains(u.Id)) != ids.Count) return "Chỉ xếp hạng sinh viên hợp lệ đã chọn đề tài này.";
            var list = await db.Set<LecturerPriorityList>().FindAsync(topicId, lecturer);
            if (list?.Version != version) return "Danh sách đã thay đổi. Vui lòng tải lại trang.";
            if (list == null) { list = new LecturerPriorityList { TopicId = topicId, LecturerId = lecturer }; db.Add(list); }
            else await db.Set<LecturerStudentPriority>().Where(p => p.TopicId == topicId && p.LecturerId == lecturer).ExecuteDeleteAsync();
            list.Reason = reason.Trim(); list.Version = Guid.NewGuid().ToString("N"); list.SavedAt = DateTime.UtcNow;
            db.AddRange(ids.Select((id, index) => new LecturerStudentPriority { TopicId = topicId, LecturerId = lecturer, StudentId = id, Rank = index + 1 }));
            return null;
        }, true);
    }
}
