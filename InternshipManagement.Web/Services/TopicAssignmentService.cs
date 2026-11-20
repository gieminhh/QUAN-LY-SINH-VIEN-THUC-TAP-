
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
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

    public async Task<AssignResult> AssignAsync(
        ulong topicId,
        string studentUserId,
        string staffUserId,
        string? adjustmentReason = null,
        ulong? groupId = null)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var topic = await _db.Topics
            .FirstOrDefaultAsync(t => t.Id == topicId);

        if (topic == null)
            return new AssignResult(false, "Không tìm thấy đề tài.");

        if (!await _scopeService.CanManageCampaignAsync(
            staffUserId, topic.CampaignId))
            return new AssignResult(false, "Bạn không có quyền quản lý đợt thực tập này.");

        // Không cho một sinh viên tham gia nhiều đề tài trong cùng đợt
        var alreadyAssigned = await _db.TopicStudentAssignments
            .AnyAsync(a =>
                a.StudentUserId == studentUserId &&
                a.Topic != null &&
                a.Topic.CampaignId == topic.CampaignId);

        if (alreadyAssigned)
            return new AssignResult(false, "Sinh viên đã được ghép vào đề tài khác trong đợt này.");

        if (topic.TopicType == TopicTypes.Individual)
        {
            if (groupId.HasValue)
                return new AssignResult(false, "Đề tài cá nhân không sử dụng nhóm.");

            if (topic.MaxStudents.HasValue)
            {
                var current = await _db.TopicStudentAssignments
                    .CountAsync(a => a.TopicId == topicId);

                if (current >= topic.MaxStudents.Value)
                    return new AssignResult(false, "Đề tài đã đủ chỉ tiêu sinh viên.");
            }
        }
        else if (topic.TopicType == TopicTypes.Group)
        {
            if (!groupId.HasValue)
                return new AssignResult(false, "Vui lòng chọn nhóm.");

            var group = await _db.TopicAssignmentGroups
                .FirstOrDefaultAsync(g =>
                    g.Id == groupId.Value &&
                    g.TopicId == topicId);

            if (group == null)
                return new AssignResult(false, "Nhóm không thuộc đề tài này.");

            if (group.IsLocked)
                return new AssignResult(false, "Nhóm đã bị khóa.");

            if (topic.MaxGroups.HasValue)
            {
                var currentGroups = await _db.TopicAssignmentGroups
                    .CountAsync(g => g.TopicId == topicId);

                if (currentGroups > topic.MaxGroups.Value)
                    return new AssignResult(false, "Đã vượt quá số nhóm cho phép.");
            }
        }
        else
        {
            return new AssignResult(false, "Loại đề tài không hợp lệ.");
        }

        _db.TopicStudentAssignments.Add(new TopicStudentAssignment
        {
            TopicId = topicId,
            StudentUserId = studentUserId,
            GroupId = groupId,
            AdjustmentReason = adjustmentReason,
            AdjustedBy = staffUserId,
            AdjustedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return new AssignResult(true);
    }

    public async Task<AssignResult> RemoveAsync(
        ulong assignmentId,
        string staffUserId,
        string adjustmentReason)
    {
        var assignment = await _db.TopicStudentAssignments
            .Include(a => a.Topic)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null)
            return new AssignResult(false, "Không tìm thấy cặp ghép.");

        if (assignment.Topic == null ||
            !await _scopeService.CanManageCampaignAsync(
                staffUserId, assignment.Topic.CampaignId))
            return new AssignResult(false, "Bạn không có quyền gỡ cặp này.");

        if (assignment.IsLocked)
            return new AssignResult(false, "Cặp ghép đã khóa, không thể gỡ.");

        if (string.IsNullOrWhiteSpace(adjustmentReason))
            return new AssignResult(false, "Vui lòng nhập lý do gỡ cặp.");

        _db.TopicStudentAssignments.Remove(assignment);
        await _db.SaveChangesAsync();

        return new AssignResult(true);
    }

    public async Task<AssignResult> LockAsync(
        ulong assignmentId,
        string staffUserId)
    {
        var assignment = await _db.TopicStudentAssignments
            .Include(a => a.Topic)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null)
            return new AssignResult(false, "Không tìm thấy cặp ghép.");

        if (assignment.Topic == null ||
            !await _scopeService.CanManageCampaignAsync(
                staffUserId, assignment.Topic.CampaignId))
            return new AssignResult(false, "Bạn không có quyền khóa cặp này.");

        assignment.IsLocked = true;
        assignment.AdjustedBy = staffUserId;
        assignment.AdjustedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return new AssignResult(true);
    }

    public async Task<AssignResult> UnlockAsync(
        ulong assignmentId,
        string staffUserId)
    {
        var assignment = await _db.TopicStudentAssignments
            .Include(a => a.Topic)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null)
            return new AssignResult(false, "Không tìm thấy cặp ghép.");

        if (assignment.Topic == null ||
            !await _scopeService.CanManageCampaignAsync(
                staffUserId, assignment.Topic.CampaignId))
            return new AssignResult(false, "Bạn không có quyền mở khóa cặp này.");

        assignment.IsLocked = false;
        assignment.AdjustedBy = staffUserId;
        assignment.AdjustedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return new AssignResult(true);
    }
}
