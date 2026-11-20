using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Web.Services;

public record AssignResult(bool Success, string? Error = null);

public interface ITopicAssignmentService
{
    Task<AssignResult> AssignAsync(ulong topicId, string studentUserId);
}

public class TopicAssignmentService : ITopicAssignmentService
{
    private readonly ApplicationDbContext _db;

    public TopicAssignmentService(ApplicationDbContext db) => _db = db;

    public async Task<AssignResult> AssignAsync(ulong topicId, string studentUserId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        // Khóa dòng đề tài: các yêu cầu cùng đề tài sẽ chạy lần lượt
        var topic = await _db.Topics
            .FromSqlInterpolated($"SELECT * FROM topics WHERE id = {topicId} FOR UPDATE")
            .FirstOrDefaultAsync();

        if (topic is null)
            return new AssignResult(false, "Không tìm thấy đề tài.");

        var already = await _db.TopicStudentAssignments
            .AnyAsync(a => a.TopicId == topicId && a.StudentUserId == studentUserId);
        if (already)
            return new AssignResult(false, "Sinh viên đã được ghép vào đề tài này.");

        // MaxStudents = null nghĩa là không giới hạn
        if (topic.MaxStudents.HasValue)
        {
            var current = await _db.TopicStudentAssignments.CountAsync(a => a.TopicId == topicId);
            if (current >= topic.MaxStudents.Value)
                return new AssignResult(false, "Đề tài đã đủ chỉ tiêu.");
        }

        _db.TopicStudentAssignments.Add(new TopicStudentAssignment
        {
            TopicId = topicId,
            StudentUserId = studentUserId
        });
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return new AssignResult(true);
    }
}