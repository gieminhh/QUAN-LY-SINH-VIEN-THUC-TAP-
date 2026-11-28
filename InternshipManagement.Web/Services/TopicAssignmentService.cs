using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services.Matching;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Web.Services;

public class TopicAssignmentService : ITopicAssignmentService
{
    private readonly ApplicationDbContext _db;
    private readonly IUserScopeService _scopeService;

    public TopicAssignmentService(
        ApplicationDbContext db,
        IUserScopeService scopeService)
    {
        _db = db;
        _scopeService = scopeService;
    }

    // =====================================================
    // LẤY NGUYỆN VỌNG HỢP LỆ
    // =====================================================

    public Task<List<StudentTopicPreference>> GetValidPreferencesAsync(
        string studentId,
        ulong campaignId)
    {
        return _db.StudentTopicPreferences
            .Include(p => p.Topic)
            .Where(p =>
                p.StudentId == studentId &&
                p.Topic != null &&
                p.Topic.CampaignId == campaignId &&
                p.Topic.ApprovalStatus == TopicApprovalStatuses.Approved)
            .OrderBy(p => p.Priority)
            .ToListAsync();
    }

    // =====================================================
    // GHÉP TỰ ĐỘNG
    // =====================================================

    public async Task RunMatchingAsync(ulong campaignId)
    {
        var campaign = await _db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId)
            ?? throw new InvalidOperationException("Không tìm thấy đợt ghép.");

        var studentRoleId = await _db.Roles
            .Where(r => r.Name == AppRoles.Student)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        if (studentRoleId is null)
            throw new InvalidOperationException("Không tìm thấy role sinh viên.");

        var studentsQuery = _db.Users
            .Where(u =>
                u.MajorId == campaign.MajorId &&
                _db.UserRoles.Any(r =>
                    r.UserId == u.Id &&
                    r.RoleId == studentRoleId));

        var random = campaign.RandomSeed.HasValue
            ? new Random(campaign.RandomSeed.Value)
            : new Random();

        List<string> studentIds;

        switch (campaign.MatchingOrder)
        {
            case MatchingOrder.RegistrationTime:

                studentIds = await studentsQuery
                    .Select(u => new
                    {
                        u.Id,
                        RegisteredAt = _db.StudentTopicPreferences
                            .Where(p =>
                                p.StudentId == u.Id &&
                                p.Topic != null &&
                                p.Topic.CampaignId == campaignId)
                            .Select(p => (DateTime?)p.CreatedAt)
                            .Min()
                    })
                    .OrderBy(s => s.RegisteredAt == null)
                    .ThenBy(s => s.RegisteredAt)
                    .ThenBy(s => s.Id)
                    .Select(s => s.Id)
                    .ToListAsync();

                break;

            case MatchingOrder.Random:

                studentIds = await studentsQuery
                    .OrderBy(u => u.Id)
                    .Select(u => u.Id)
                    .ToListAsync();

                for (var i = studentIds.Count - 1; i > 0; i--)
                {
                    var j = random.Next(i + 1);
                    (studentIds[i], studentIds[j]) =
                        (studentIds[j], studentIds[i]);
                }

                break;

            default:
                throw new InvalidOperationException(
                    "Thứ tự xét sinh viên không hợp lệ.");
        }

        var noPreferences = new List<string>();
        var studentsWithPreferences = new List<string>();

        var preferencesByStudent =
            new Dictionary<string, List<ulong>>();

        foreach (var studentId in studentIds)
        {
            var existing = await _db.TopicStudentAssignments
                .FirstOrDefaultAsync(a =>
                    a.CampaignId == campaignId &&
                    a.StudentUserId == studentId);

            // Không tự động thay đổi cặp đã ghép hoặc đã khóa
            if (existing?.Status == MatchStatus.Matched ||
                existing?.IsLocked == true)
            {
                continue;
            }

            var preferences = await GetValidPreferencesAsync(
                studentId, campaignId);

            if (preferences.Count == 0)
            {
                noPreferences.Add(studentId);
                continue;
            }

            studentsWithPreferences.Add(studentId);

            preferencesByStudent[studentId] = preferences
                .Select(p => p.TopicId)
                .ToList();
        }

        var preferredTopicIds = preferencesByStudent.Values
            .SelectMany(ids => ids)
            .Distinct()
            .ToList();

        var topics = await _db.Topics
            .Where(t =>
                t.CampaignId == campaignId &&
                t.ApprovalStatus == TopicApprovalStatuses.Approved &&
                preferredTopicIds.Contains(t.Id))
            .Select(t => new
            {
                t.Id,
                t.MaxStudents
            })
            .ToListAsync();

        var currentAssignmentCounts = await _db.TopicStudentAssignments
            .Where(a =>
                a.CampaignId == campaignId &&
                a.Status == MatchStatus.Matched &&
                a.TopicId.HasValue &&
                preferredTopicIds.Contains(a.TopicId.Value))
            .GroupBy(a => a.TopicId!.Value)
            .Select(g => new
            {
                TopicId = g.Key,
                Count = g.Count()
            })
            .ToDictionaryAsync(
                x => x.TopicId,
                x => x.Count);

        var topicCapacities = topics.ToDictionary(
            t => t.Id,
            t => t.MaxStudents.HasValue
                ? Math.Max(
                    0,
                    t.MaxStudents.Value -
                    currentAssignmentCounts.GetValueOrDefault(t.Id))
                : studentsWithPreferences.Count);

        var matching = GreedyMatcher.Match(
            studentsWithPreferences,
            preferencesByStudent,
            topicCapacities);

        var unmatchedStudentIds =
            matching.UnmatchedStudentIds.ToHashSet();

        foreach (var (studentId, topicId) in matching.Matches)
        {
            var result = await AssignAsync(studentId, topicId);

            if (!result.ok)
            {
                if (result.error != "Đề tài đã đủ chỉ tiêu.")
                    throw new InvalidOperationException(result.error);

                unmatchedStudentIds.Add(studentId);
            }
        }

        foreach (var studentId in unmatchedStudentIds)
        {
            await SetUnmatchedStatusAsync(
                studentId,
                campaignId,
                MatchStatus.Unmatched);
        }

        // Xử lý sinh viên không có nguyện vọng
        foreach (var studentId in noPreferences)
        {
            var status = campaign.NoPreferencePolicy switch
            {
                NoPreferencePolicy.ManualByAdmin =>
                    MatchStatus.PendingManual,

                _ => MatchStatus.Unmatched
            };

            if (campaign.NoPreferencePolicy ==
                NoPreferencePolicy.RandomFill)
            {
                var availableTopics = await _db.Topics
                    .Where(t =>
                        t.CampaignId == campaignId &&
                        t.ApprovalStatus == TopicApprovalStatuses.Approved)
                    .OrderBy(t => t.Id)
                    .ToListAsync();

                while (availableTopics.Count > 0)
                {
                    var index = random.Next(availableTopics.Count);
                    var topic = availableTopics[index];

                    availableTopics.RemoveAt(index);

                    if (topic.MaxStudents.HasValue)
                    {
                        var current = await _db.TopicStudentAssignments
                            .CountAsync(a =>
                                a.TopicId == topic.Id &&
                                a.Status == MatchStatus.Matched);

                        if (current >= topic.MaxStudents.Value)
                            continue;
                    }

                    var result = await AssignAsync(
                        studentId,
                        topic.Id);

                    if (result.ok)
                    {
                        status = MatchStatus.Matched;
                        break;
                    }

                    if (result.error != "Đề tài đã đủ chỉ tiêu.")
                        throw new InvalidOperationException(result.error);
                }
            }

            if (status != MatchStatus.Matched)
            {
                await SetUnmatchedStatusAsync(
                    studentId,
                    campaignId,
                    status);
            }
        }
    }

    // =====================================================
    // GHÉP TỰ ĐỘNG MỘT SINH VIÊN
    // =====================================================

    public async Task<(bool ok, string? error)> AssignAsync(
        string studentId,
        ulong topicId)
    {
        await using var tx =
            await _db.Database.BeginTransactionAsync();

        var topic = await _db.Topics
            .FromSqlInterpolated(
                $"SELECT * FROM topics WHERE id = {topicId} FOR UPDATE")
            .FirstOrDefaultAsync();

        if (topic == null)
            return (false, "Không tìm thấy đề tài.");

        var assignment = await _db.TopicStudentAssignments
            .FirstOrDefaultAsync(a =>
                a.CampaignId == topic.CampaignId &&
                a.StudentUserId == studentId);

        if (assignment?.Status == MatchStatus.Matched)
            return (false, "Sinh viên đã được ghép trong đợt này.");

        if (assignment?.IsLocked == true)
            return (false, "Cặp ghép đã khóa.");

        if (topic.MaxStudents.HasValue)
        {
            var current = await _db.TopicStudentAssignments
                .CountAsync(a =>
                    a.TopicId == topicId &&
                    a.Status == MatchStatus.Matched);

            if (current >= topic.MaxStudents.Value)
                return (false, "Đề tài đã đủ chỉ tiêu.");
        }

        if (assignment == null)
        {
            assignment = new TopicStudentAssignment
            {
                CampaignId = topic.CampaignId,
                StudentUserId = studentId
            };

            _db.TopicStudentAssignments.Add(assignment);
        }

        assignment.CampaignId = topic.CampaignId;
        assignment.TopicId = topicId;
        assignment.GroupId = null;
        assignment.Status = MatchStatus.Matched;
        assignment.AssignedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await AddHistoryAsync(
            assignment,
            "AUTO_MATCH",
            null,
            "SYSTEM");

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return (true, null);
    }

    // =====================================================
    // GHÉP THỦ CÔNG
    // =====================================================

    public async Task<AssignResult> AssignAsync(
        ulong topicId,
        string studentUserId,
        string staffUserId,
        string? adjustmentReason = null,
        ulong? groupId = null,
        string? groupName = null)
    {
        await using var tx =
            await _db.Database.BeginTransactionAsync();

        var topic = await _db.Topics
            .FromSqlInterpolated(
                $"SELECT * FROM topics WHERE id = {topicId} FOR UPDATE")
            .FirstOrDefaultAsync();

        if (topic == null)
            return new AssignResult(false, "Không tìm thấy đề tài.");

        if (!await _scopeService.CanManageCampaignAsync(
            staffUserId,
            topic.CampaignId))
        {
            return new AssignResult(
                false,
                "Bạn không có quyền quản lý đợt thực tập này.");
        }

        var assignment = await _db.TopicStudentAssignments
            .FirstOrDefaultAsync(a =>
                a.CampaignId == topic.CampaignId &&
                a.StudentUserId == studentUserId);

        if (assignment?.Status == MatchStatus.Matched)
        {
            return new AssignResult(
                false,
                "Sinh viên đã được ghép vào đề tài trong đợt này.");
        }

        if (assignment?.IsLocked == true)
            return new AssignResult(false, "Cặp ghép đã bị khóa.");

        TopicAssignmentGroup? group = null;

        if (topic.TopicType == TopicTypes.Individual)
        {
            if (groupId.HasValue ||
                !string.IsNullOrWhiteSpace(groupName))
            {
                return new AssignResult(
                    false,
                    "Đề tài cá nhân không sử dụng nhóm.");
            }

            if (topic.MaxStudents.HasValue)
            {
                var current = await _db.TopicStudentAssignments
                    .CountAsync(a =>
                        a.TopicId == topicId &&
                        a.Status == MatchStatus.Matched);

                if (current >= topic.MaxStudents.Value)
                {
                    return new AssignResult(
                        false,
                        "Đề tài đã đủ chỉ tiêu sinh viên.");
                }
            }
        }
        else if (topic.TopicType == TopicTypes.Group)
        {
            if (groupId.HasValue)
            {
                group = await _db.TopicAssignmentGroups
                    .FirstOrDefaultAsync(g =>
                        g.Id == groupId.Value &&
                        g.TopicId == topicId);

                if (group == null)
                {
                    return new AssignResult(
                        false,
                        "Nhóm không thuộc đề tài này.");
                }

                if (group.IsLocked)
                    return new AssignResult(false, "Nhóm đã bị khóa.");
            }
            else if (!string.IsNullOrWhiteSpace(groupName))
            {
                var currentGroups = await _db.TopicAssignmentGroups
                    .CountAsync(g => g.TopicId == topicId);

                if (topic.MaxGroups.HasValue &&
                    currentGroups >= topic.MaxGroups.Value)
                {
                    return new AssignResult(
                        false,
                        "Đề tài đã đạt số nhóm tối đa.");
                }

                group = new TopicAssignmentGroup
                {
                    TopicId = topicId,
                    GroupName = groupName.Trim()
                };

                _db.TopicAssignmentGroups.Add(group);
            }
            else
            {
                return new AssignResult(
                    false,
                    "Vui lòng chọn nhóm hoặc nhập tên nhóm mới.");
            }
        }
        else
        {
            return new AssignResult(
                false,
                "Loại đề tài không hợp lệ.");
        }

        if (assignment == null)
        {
            assignment = new TopicStudentAssignment
            {
                CampaignId = topic.CampaignId,
                StudentUserId = studentUserId
            };

            _db.TopicStudentAssignments.Add(assignment);
        }

        assignment.CampaignId = topic.CampaignId;
        assignment.TopicId = topicId;
        assignment.Group = group;
        assignment.GroupId = group?.Id;
        assignment.Status = MatchStatus.Matched;
        assignment.AssignedAt = DateTime.UtcNow;
        assignment.AdjustmentReason = adjustmentReason;
        assignment.AdjustedBy = staffUserId;
        assignment.AdjustedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await AddHistoryAsync(
            assignment,
            "MANUAL_ASSIGN",
            adjustmentReason,
            staffUserId);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return new AssignResult(true);
    }

    // =====================================================
    // GỠ CẶP
    // =====================================================

    public async Task<AssignResult> RemoveAsync(
        ulong assignmentId,
        string staffUserId,
        string adjustmentReason)
    {
        if (string.IsNullOrWhiteSpace(adjustmentReason))
        {
            return new AssignResult(
                false,
                "Vui lòng nhập lý do gỡ cặp.");
        }

        await using var tx =
            await _db.Database.BeginTransactionAsync();

        var assignment = await _db.TopicStudentAssignments
            .Include(a => a.Topic)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null)
            return new AssignResult(false, "Không tìm thấy cặp ghép.");

        if (assignment.Topic == null ||
            !await _scopeService.CanManageCampaignAsync(
                staffUserId,
                assignment.Topic.CampaignId))
        {
            return new AssignResult(
                false,
                "Bạn không có quyền gỡ cặp này.");
        }

        if (assignment.IsLocked)
        {
            return new AssignResult(
                false,
                "Cặp ghép đã khóa, không thể gỡ.");
        }

        await AddHistoryAsync(
            assignment,
            "REMOVE",
            adjustmentReason,
            staffUserId);

        _db.TopicStudentAssignments.Remove(assignment);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return new AssignResult(true);
    }

    // =====================================================
    // KHÓA CẶP
    // =====================================================

    public async Task<AssignResult> LockAsync(
        ulong assignmentId,
        string staffUserId,
        string? adjustmentReason = null)
    {
        var assignment = await _db.TopicStudentAssignments
            .Include(a => a.Topic)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null)
            return new AssignResult(false, "Không tìm thấy cặp ghép.");

        if (assignment.Topic == null ||
            !await _scopeService.CanManageCampaignAsync(
                staffUserId,
                assignment.Topic.CampaignId))
        {
            return new AssignResult(
                false,
                "Bạn không có quyền khóa cặp này.");
        }

        if (assignment.IsLocked)
            return new AssignResult(false, "Cặp ghép đã được khóa.");

        assignment.IsLocked = true;
        assignment.AdjustmentReason = adjustmentReason;
        assignment.AdjustedBy = staffUserId;
        assignment.AdjustedAt = DateTime.UtcNow;

        await AddHistoryAsync(
            assignment,
            "LOCK",
            adjustmentReason,
            staffUserId);

        await _db.SaveChangesAsync();

        return new AssignResult(true);
    }

    // =====================================================
    // MỞ KHÓA CẶP
    // =====================================================

    public async Task<AssignResult> UnlockAsync(
        ulong assignmentId,
        string staffUserId,
        string? adjustmentReason = null)
    {
        var assignment = await _db.TopicStudentAssignments
            .Include(a => a.Topic)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null)
            return new AssignResult(false, "Không tìm thấy cặp ghép.");

        if (assignment.Topic == null ||
            !await _scopeService.CanManageCampaignAsync(
                staffUserId,
                assignment.Topic.CampaignId))
        {
            return new AssignResult(
                false,
                "Bạn không có quyền mở khóa cặp này.");
        }

        if (!assignment.IsLocked)
            return new AssignResult(false, "Cặp ghép hiện không bị khóa.");

        assignment.IsLocked = false;
        assignment.AdjustmentReason = adjustmentReason;
        assignment.AdjustedBy = staffUserId;
        assignment.AdjustedAt = DateTime.UtcNow;

        await AddHistoryAsync(
            assignment,
            "UNLOCK",
            adjustmentReason,
            staffUserId);

        await _db.SaveChangesAsync();

        return new AssignResult(true);
    }

    // =====================================================
    // CẬP NHẬT TRẠNG THÁI CHƯA ĐƯỢC GHÉP
    // =====================================================

    private async Task SetUnmatchedStatusAsync(
        string studentId,
        ulong campaignId,
        MatchStatus status)
    {
        var assignment = await _db.TopicStudentAssignments
            .FirstOrDefaultAsync(a =>
                a.CampaignId == campaignId &&
                a.StudentUserId == studentId);

        if (assignment?.IsLocked == true ||
            assignment?.Status == MatchStatus.Matched)
        {
            return;
        }

        if (assignment == null)
        {
            assignment = new TopicStudentAssignment
            {
                CampaignId = campaignId,
                StudentUserId = studentId,
                TopicId = null,
                Status = status
            };

            _db.TopicStudentAssignments.Add(assignment);
        }
        else
        {
            assignment.TopicId = null;
            assignment.GroupId = null;
            assignment.Status = status;
        }

        await _db.SaveChangesAsync();
    }

    // =====================================================
    // GHI LỊCH SỬ ĐIỀU CHỈNH
    // =====================================================

    private Task AddHistoryAsync(
        TopicStudentAssignment assignment,
        string action,
        string? reason,
        string changedBy)
    {
        if (!assignment.TopicId.HasValue)
        {
            throw new InvalidOperationException(
                "Không thể ghi lịch sử cho bản ghi chưa có đề tài.");
        }

        _db.TopicAssignmentHistories.Add(
            new TopicAssignmentHistory
            {
                AssignmentId = assignment.Id,
                TopicId = assignment.TopicId.Value,
                StudentUserId = assignment.StudentUserId,
                GroupId = assignment.GroupId,
                Action = action,
                Reason = reason,
                ChangedBy = changedBy,
                ChangedAt = DateTime.UtcNow
            });

        return Task.CompletedTask;
    }
}