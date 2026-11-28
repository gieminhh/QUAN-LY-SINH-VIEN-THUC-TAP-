using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
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
    private readonly IMatchingPreviewService _preview;

    public TopicAssignmentService(ApplicationDbContext db, IMatchingPreviewService preview)
    {
        _db = db;
        _preview = preview;
    }

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
        var preview = await _preview.PreviewAsync(campaignId)
            ?? throw new InvalidOperationException("Không tìm thấy đợt ghép.");
        if (preview.Errors.Count > 0) throw new InvalidOperationException(preview.Errors[0]);
        if (preview.Changes == 0) return;
        var result = await _preview.ConfirmAsync(campaignId, preview.Input.Seed, preview.Input.Fingerprint);
        if (!result.Ok) throw new InvalidOperationException(result.Error);
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

}
