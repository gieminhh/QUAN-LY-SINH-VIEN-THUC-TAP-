using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services.Matching;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Web.Services;

public interface ITopicAssignmentService
{
    Task<List<StudentTopicPreference>> GetValidPreferencesAsync(string studentId, ulong campaignId);
    Task RunMatchingAsync(ulong campaignId);
    Task<(bool ok, string? error)> AssignAsync(string studentId, ulong topicId);
}

public class TopicAssignmentService : ITopicAssignmentService
{
    private readonly ApplicationDbContext _db;

    public TopicAssignmentService(ApplicationDbContext db) => _db = db;

    public Task<List<StudentTopicPreference>> GetValidPreferencesAsync(string studentId, ulong campaignId)
    {
        return _db.StudentTopicPreferences
            .Include(preference => preference.Topic)
            .Where(preference =>
                preference.StudentId == studentId &&
                preference.Topic != null &&
                preference.Topic.CampaignId == campaignId &&
                preference.Topic.ApprovalStatus == TopicApprovalStatuses.Approved)
            .OrderBy(preference => preference.Priority)
            .ToListAsync();
    }

    public async Task RunMatchingAsync(ulong campaignId)
    {
        var campaign = await _db.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId)
            ?? throw new InvalidOperationException("Không tìm thấy đợt ghép.");

        var studentRoleId = await _db.Roles
            .Where(role => role.Name == AppRoles.Student)
            .Select(role => role.Id)
            .FirstOrDefaultAsync();
        if (studentRoleId is null)
            throw new InvalidOperationException("Không tìm thấy role sinh viên.");

        var studentsQuery = _db.Users
            .Where(user => user.MajorId == campaign.MajorId &&
                _db.UserRoles.Any(role => role.UserId == user.Id && role.RoleId == studentRoleId));

        var random = campaign.RandomSeed.HasValue ? new Random(campaign.RandomSeed.Value) : new Random();
        List<string> studentIds;
        switch (campaign.MatchingOrder)
        {
            case MatchingOrder.RegistrationTime:
                studentIds = await studentsQuery
                    .Select(user => new
                    {
                        user.Id,
                        RegisteredAt = _db.StudentTopicPreferences
                            .Where(preference =>
                                preference.StudentId == user.Id &&
                                preference.Topic != null &&
                                preference.Topic.CampaignId == campaignId)
                            .Select(preference => (DateTime?)preference.CreatedAt)
                            .Min()
                    })
                    .OrderBy(student => student.RegisteredAt == null)
                    .ThenBy(student => student.RegisteredAt)
                    .ThenBy(student => student.Id)
                    .Select(student => student.Id)
                    .ToListAsync();
                break;
            case MatchingOrder.Random:
                studentIds = await studentsQuery
                    .OrderBy(user => user.Id)
                    .Select(user => user.Id)
                    .ToListAsync();
                for (var i = studentIds.Count - 1; i > 0; i--)
                {
                    var j = random.Next(i + 1);
                    (studentIds[i], studentIds[j]) = (studentIds[j], studentIds[i]);
                }
                break;
            default:
                throw new InvalidOperationException("Thứ tự xét sinh viên không hợp lệ.");
        }

        var noPreferences = new List<string>();
        var studentsWithPreferences = new List<string>();
        var preferencesByStudent = new Dictionary<string, List<ulong>>();
        foreach (var studentId in studentIds)
        {
            var existing = await _db.TopicStudentAssignments
                .FirstOrDefaultAsync(a => a.CampaignId == campaignId && a.StudentUserId == studentId);
            if (existing?.Status == MatchStatus.Matched)
                continue;

            var preferences = await GetValidPreferencesAsync(studentId, campaignId);
            if (preferences.Count == 0)
            {
                noPreferences.Add(studentId);
                continue;
            }

            studentsWithPreferences.Add(studentId);
            preferencesByStudent[studentId] = preferences
                .Select(preference => preference.TopicId)
                .ToList();
        }

        var preferredTopicIds = preferencesByStudent.Values
            .SelectMany(topicIds => topicIds)
            .Distinct()
            .ToList();
        var topics = await _db.Topics
            .Where(topic =>
                topic.CampaignId == campaignId &&
                topic.ApprovalStatus == TopicApprovalStatuses.Approved &&
                preferredTopicIds.Contains(topic.Id))
            .Select(topic => new { topic.Id, topic.MaxStudents })
            .ToListAsync();
        var currentAssignmentCounts = await _db.TopicStudentAssignments
            .Where(assignment =>
                assignment.CampaignId == campaignId &&
                assignment.Status == MatchStatus.Matched &&
                assignment.TopicId.HasValue &&
                preferredTopicIds.Contains(assignment.TopicId.Value))
            .GroupBy(assignment => assignment.TopicId!.Value)
            .Select(group => new { TopicId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.TopicId, group => group.Count);
        var topicCapacities = topics.ToDictionary(
            topic => topic.Id,
            topic => topic.MaxStudents.HasValue
                ? Math.Max(0, topic.MaxStudents.Value - currentAssignmentCounts.GetValueOrDefault(topic.Id))
                : studentsWithPreferences.Count);
        var matching = GreedyMatcher.Match(studentsWithPreferences, preferencesByStudent, topicCapacities);
        var unmatchedStudentIds = matching.UnmatchedStudentIds.ToHashSet();

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
            await SetUnmatchedStatusAsync(studentId, campaignId, MatchStatus.Unmatched);

        foreach (var studentId in noPreferences)
        {
            var status = campaign.NoPreferencePolicy switch
            {
                NoPreferencePolicy.ManualByAdmin => MatchStatus.PendingManual,
                _ => MatchStatus.Unmatched
            };

            if (campaign.NoPreferencePolicy == NoPreferencePolicy.RandomFill)
            {
                var availableTopics = await _db.Topics
                    .Where(topic =>
                        topic.CampaignId == campaignId &&
                        topic.ApprovalStatus == TopicApprovalStatuses.Approved)
                    .OrderBy(topic => topic.Id)
                    .ToListAsync();

                while (availableTopics.Count > 0)
                {
                    var index = random.Next(availableTopics.Count);
                    var topic = availableTopics[index];
                    availableTopics.RemoveAt(index);

                    if (topic.MaxStudents.HasValue)
                    {
                        var current = await _db.TopicStudentAssignments.CountAsync(a =>
                            a.TopicId == topic.Id && a.Status == MatchStatus.Matched);
                        if (current >= topic.MaxStudents.Value)
                            continue;
                    }

                    var result = await AssignAsync(studentId, topic.Id);
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
                await SetUnmatchedStatusAsync(studentId, campaignId, status);
        }
    }

    public async Task<(bool ok, string? error)> AssignAsync(string studentId, ulong topicId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        // Khóa dòng đề tài: các yêu cầu cùng đề tài sẽ chạy lần lượt
        var topic = await _db.Topics
            .FromSqlInterpolated($"SELECT * FROM topics WHERE id = {topicId} FOR UPDATE")
            .FirstOrDefaultAsync();

        if (topic is null)
            return (false, "Không tìm thấy đề tài.");

        var assignment = await _db.TopicStudentAssignments
            .FirstOrDefaultAsync(a => a.CampaignId == topic.CampaignId && a.StudentUserId == studentId);
        if (assignment?.Status == MatchStatus.Matched)
            return (false, "Sinh viên đã được ghép trong đợt này.");

        // MaxStudents = null nghĩa là không giới hạn
        if (topic.MaxStudents.HasValue)
        {
            var current = await _db.TopicStudentAssignments.CountAsync(a =>
                a.TopicId == topicId && a.Status == MatchStatus.Matched);
            if (current >= topic.MaxStudents.Value)
                return (false, "Đề tài đã đủ chỉ tiêu.");
        }

        if (assignment is null)
        {
            assignment = new TopicStudentAssignment
            {
                CampaignId = topic.CampaignId,
                StudentUserId = studentId
            };
            _db.TopicStudentAssignments.Add(assignment);
        }

        assignment.TopicId = topicId;
        assignment.Status = MatchStatus.Matched;
        assignment.AssignedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return (true, null);
    }

    private async Task SetUnmatchedStatusAsync(string studentId, ulong campaignId, MatchStatus status)
    {
        var assignment = await _db.TopicStudentAssignments
            .FirstOrDefaultAsync(a => a.CampaignId == campaignId && a.StudentUserId == studentId);

        if (assignment is null)
        {
            _db.TopicStudentAssignments.Add(new TopicStudentAssignment
            {
                CampaignId = campaignId,
                StudentUserId = studentId,
                Status = status
            });
        }
        else
        {
            assignment.TopicId = null;
            assignment.Status = status;
        }

        await _db.SaveChangesAsync();
    }
}
