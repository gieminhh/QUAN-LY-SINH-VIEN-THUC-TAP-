using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services.Matching;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Web.Services;

public sealed class MatchingPublicationService(ApplicationDbContext db, IUserScopeService scopes, TimeProvider clock)
{
    public DateTime UtcNow => clock.GetUtcNow().UtcDateTime;
    public Task<bool> HasRole(string actor, string role) => db.Users.AnyAsync(u => u.Id == actor && u.IsActive &&
        db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == role)));
    public async Task<bool> CanManage(string actor, ulong campaignId) =>
        (await HasRole(actor, AppRoles.Admin) || await HasRole(actor, AppRoles.TrainingManager) || await HasRole(actor, AppRoles.FacultyManager)) &&
        await scopes.CanReviewCampaignAsync(actor, campaignId);

    public async Task<PublicationPlan?> Review(ulong campaignId)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        var input = await Load(campaignId);
        await tx.CommitAsync();
        return input is null ? null : PublicationPlanner.Build(input);
    }

    private async Task<Campaign?> Lock(ulong campaignId) => (await db.Campaigns
        .FromSqlInterpolated($"SELECT * FROM campaigns WHERE id = {campaignId} FOR UPDATE").ToListAsync()).SingleOrDefault();

    public async Task<string?> Publish(string actor, ulong campaignId, string fingerprint)
    {
        if (!await CanManage(actor, campaignId)) return "Bạn không có quyền duyệt công bố đợt này.";
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var campaign = await Lock(campaignId);
        if (campaign is null) return "Không tìm thấy đợt.";
        await db.Topics.FromSqlInterpolated($"SELECT * FROM topics WHERE campaign_id = {campaignId} ORDER BY id FOR UPDATE")
            .AsNoTracking().ToListAsync();
        var rounds = await db.Set<MatchingRound>().Where(r => r.CampaignId == campaignId).OrderBy(r => r.Number).ToListAsync();
        var round = rounds.LastOrDefault();
        if (round?.State == MatchingRoundState.Published) return "Kết quả đã công bố. Hãy mở vòng bổ sung nếu cần.";
        var input = (await Load(campaignId))!;
        if (input.Fingerprint != fingerprint) return "Dữ liệu đã thay đổi. Vui lòng xem lại trước khi duyệt công bố.";
        var plan = PublicationPlanner.Build(input);
        if (!plan.CanPublish(UtcNow)) return plan.Errors.FirstOrDefault() ?? "Chỉ công bố khi đợt đã đóng, hết hạn vòng bổ sung và có sinh viên xét ghép.";
        // Permission may have been revoked while the request waited for database locks.
        if (!await CanManage(actor, campaignId)) return "Quyền duyệt công bố đã thay đổi.";
        if (round is null)
        {
            round = new MatchingRound { CampaignId = campaignId, Number = 1, OpenedBy = actor, OpenedAt = UtcNow };
            db.Add(round);
            await db.SaveChangesAsync();
        }
        if (round.Number > 1)
        {
            var assignments = await db.TopicStudentAssignments.Where(a => a.CampaignId == campaignId).ToDictionaryAsync(a => a.StudentUserId);
            foreach (var row in plan.Rows)
            {
                if (!assignments.TryGetValue(row.StudentId, out var assignment))
                {
                    assignment = new TopicStudentAssignment { CampaignId = campaignId, StudentUserId = row.StudentId };
                    db.Add(assignment);
                }
                else if (assignment.Status == MatchStatus.Matched) continue;
                assignment.TopicId = row.TopicId;
                assignment.Status = row.Status;
                assignment.AssignedAt = UtcNow;
            }
        }
        db.AddRange(plan.Rows.Select(r => new MatchingPublishedResult
        {
            RoundId = round.Id, StudentId = r.StudentId, TopicId = r.TopicId,
            TopicTitle = r.TopicTitle, Status = r.Status, ConfirmedAt = r.ConfirmedAt
        }));
        round.State = MatchingRoundState.Published;
        round.PublishedAt = UtcNow;
        round.PublishedBy = actor;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return null;
    }

    public async Task<string?> OpenSupplementary(string actor, ulong campaignId, ulong previousRoundId, DateTime closesAtUtc)
    {
        if (!await CanManage(actor, campaignId)) return "Bạn không có quyền mở vòng bổ sung.";
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var campaign = await Lock(campaignId);
        if (campaign?.Status != CampaignStatuses.Closed) return "Chỉ mở vòng bổ sung khi đợt đã đóng đăng ký chính thức.";
        if (!await CanManage(actor, campaignId)) return "Quyền mở vòng bổ sung đã thay đổi.";
        var latest = await db.Set<MatchingRound>().Where(r => r.CampaignId == campaignId).OrderByDescending(r => r.Number).FirstOrDefaultAsync();
        if (latest is null || latest.Id != previousRoundId || latest.State != MatchingRoundState.Published)
            return "Trạng thái vòng đã thay đổi hoặc chưa có kết quả công bố.";
        var campaignEndUtc = campaign.EndDate.Date.AddDays(1).AddHours(-7);
        if (closesAtUtc <= UtcNow || closesAtUtc >= campaignEndUtc) return "Hạn bổ sung phải ở tương lai và nằm trong thời gian của đợt.";
        var plan = PublicationPlanner.Build((await Load(campaignId))!);
        if (plan.Errors.Count > 0) return plan.Errors[0];
        if (plan.Rows.All(r => r.Status == MatchStatus.Matched)) return "Không còn sinh viên chưa ghép.";
        if (!await AvailableTopics(campaignId).AnyAsync()) return "Không còn đề tài đã duyệt có chỉ tiêu trống.";
        db.Add(new MatchingRound { CampaignId = campaignId, Number = latest.Number + 1, State = MatchingRoundState.RegistrationOpen,
            OpenedAt = UtcNow, ClosesAt = closesAtUtc, OpenedBy = actor });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return null;
    }

    public IQueryable<Topic> AvailableTopics(ulong campaignId) => db.Topics.AsNoTracking().Where(t => t.CampaignId == campaignId &&
        t.ApprovalStatus == TopicApprovalStatuses.Approved && (!t.MaxStudents.HasValue ||
        db.TopicStudentAssignments.Count(a => a.TopicId == t.Id && a.Status == MatchStatus.Matched) < t.MaxStudents.Value));

    public static string PreferenceVersion(IEnumerable<MatchingRoundPreference> preferences) =>
        Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(preferences.OrderBy(p => p.Priority)
            .Select(p => new { p.TopicId, p.Priority, p.SubmittedAt }).ToList())));

    public async Task<string?> SavePreferences(string student, ulong roundId, List<ulong> topicIds, string version)
    {
        var info = await db.Set<MatchingRound>().AsNoTracking().SingleOrDefaultAsync(r => r.Id == roundId);
        if (info is null) return "Không tìm thấy vòng bổ sung.";
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var campaign = await Lock(info.CampaignId);
        var round = await db.Set<MatchingRound>().SingleAsync(r => r.Id == roundId);
        if (campaign?.Status != CampaignStatuses.Closed || round.State != MatchingRoundState.RegistrationOpen || UtcNow >= round.ClosesAt ||
            await db.Set<MatchingRound>().AnyAsync(r => r.CampaignId == info.CampaignId && r.Number > round.Number))
            return "Vòng bổ sung đã đóng. Không thể sửa nguyện vọng.";
        if (!await HasRole(student, AppRoles.Student) || !await db.Users.AnyAsync(u => u.Id == student && u.MajorId == campaign.MajorId))
            return "Bạn không thuộc danh sách sinh viên của đợt.";
        if (await db.TopicStudentAssignments.AnyAsync(a => a.CampaignId == campaign.Id && a.StudentUserId == student && a.Status == MatchStatus.Matched) ||
            await db.Set<MatchingPublishedResult>().AnyAsync(r => r.StudentId == student && r.Status == MatchStatus.Matched &&
                db.Set<MatchingRound>().Any(rd => rd.Id == r.RoundId && rd.CampaignId == campaign.Id && rd.State == MatchingRoundState.Published)))
            return "Bạn đã có cặp ghép, không thể đăng ký bổ sung.";
        if (topicIds.Count > 3 || topicIds.Any(t => t == 0) || topicIds.Distinct().Count() != topicIds.Count)
            return "Chọn tối đa 3 đề tài khác nhau theo thứ tự ưu tiên.";
        if (await AvailableTopics(campaign.Id).CountAsync(t => topicIds.Contains(t.Id)) != topicIds.Count)
            return "Có đề tài không thuộc đợt, chưa duyệt hoặc đã hết chỉ tiêu.";
        var existing = await db.Set<MatchingRoundPreference>().Where(p => p.RoundId == roundId && p.StudentId == student).ToListAsync();
        if (PreferenceVersion(existing) != version) return "Nguyện vọng đã thay đổi ở phiên khác. Vui lòng tải lại.";
        var submitted = existing.Count > 0 ? existing.Min(p => p.SubmittedAt) : UtcNow;
        await db.Set<MatchingRoundPreference>().Where(p => p.RoundId == roundId && p.StudentId == student).ExecuteDeleteAsync();
        foreach (var item in existing) db.Entry(item).State = EntityState.Detached;
        db.AddRange(topicIds.Select((id, i) => new MatchingRoundPreference { RoundId = roundId, StudentId = student,
            TopicId = id, Priority = i + 1, SubmittedAt = submitted }));
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return null;
    }

    public async Task<string?> Confirm(string student, ulong roundId, ulong topicId)
    {
        var info = await db.Set<MatchingRound>().AsNoTracking().SingleOrDefaultAsync(r => r.Id == roundId);
        if (info is null) return "Không tìm thấy kết quả.";
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await Lock(info.CampaignId);
        if (!await HasRole(student, AppRoles.Student)) return "Tài khoản sinh viên không hợp lệ.";
        var latest = await db.Set<MatchingRound>().AsNoTracking().Where(r => r.CampaignId == info.CampaignId && r.State == MatchingRoundState.Published)
            .OrderByDescending(r => r.Number).FirstOrDefaultAsync();
        if (latest?.Id != roundId) return "Kết quả đã thay đổi. Vui lòng tải lại bản công bố mới nhất.";
        // Student ID always comes from authentication, never from a posted field.
        var result = await db.Set<MatchingPublishedResult>().SingleOrDefaultAsync(r => r.RoundId == roundId && r.StudentId == student);
        if (result?.Status != MatchStatus.Matched || result.TopicId != topicId) return "Không tìm thấy cặp ghép đã công bố của bạn.";
        result.ConfirmedAt ??= UtcNow;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return null;
    }

    private async Task<PublicationInput?> Load(ulong campaignId)
    {
        var campaign = await db.Campaigns.AsNoTracking().SingleOrDefaultAsync(c => c.Id == campaignId);
        if (campaign is null) return null;
        var rounds = await db.Set<MatchingRound>().AsNoTracking().Where(r => r.CampaignId == campaignId).OrderBy(r => r.Number).ToListAsync();
        var active = rounds.LastOrDefault(r => r.State == MatchingRoundState.RegistrationOpen);
        var published = rounds.LastOrDefault(r => r.State == MatchingRoundState.Published);
        var students = await db.Users.AsNoTracking().Where(u => u.MajorId == campaign.MajorId && u.IsActive &&
                db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == AppRoles.Student)))
            .OrderBy(u => u.Id).Select(u => new PublicationStudent(u.Id, u.FullName, u.UserName ?? u.Id)).ToListAsync();
        var topics = await db.Topics.AsNoTracking().Where(t => t.CampaignId == campaignId).OrderBy(t => t.Id)
            .Select(t => new PublicationTopic(t.Id, t.Title, t.MaxStudents, t.ApprovalStatus)).ToListAsync();
        var assignments = await db.TopicStudentAssignments.AsNoTracking().Where(a => a.CampaignId == campaignId)
            .OrderBy(a => a.StudentUserId).ThenBy(a => a.Id).Select(a => new PublicationAssignment(a.StudentUserId, a.TopicId, a.Status)).ToListAsync();
        var preferences = active is null ? [] : await db.Set<MatchingRoundPreference>().AsNoTracking().Where(p => p.RoundId == active.Id)
            .OrderBy(p => p.StudentId).ThenBy(p => p.Priority).Select(p => new PublicationPreference(p.StudentId, p.TopicId, p.Priority, p.SubmittedAt)).ToListAsync();
        var previous = published is null ? [] : await db.Set<MatchingPublishedResult>().AsNoTracking().Where(p => p.RoundId == published.Id)
            .OrderBy(p => p.StudentId).Select(p => new PublicationPrevious(p.StudentId, p.TopicId, p.Status, p.ConfirmedAt)).ToListAsync();
        return new PublicationInput(campaignId, campaign.Status, active?.Id, active?.Number ?? published?.Number ?? 1,
            active?.ClosesAt, campaign.MatchingOrder, campaign.RandomSeed ?? unchecked((int)(active?.Id ?? 0)), students, topics, assignments, preferences, previous);
    }
}
