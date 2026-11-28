
using InternshipManagement.Web.Models;

namespace InternshipManagement.Web.Services;

public record AssignResult(bool Success, string? Error = null);

public interface ITopicAssignmentService
{
    Task<AssignResult> AssignAsync(
    ulong topicId,
    string studentUserId,
    string staffUserId,
    string? adjustmentReason = null,
    ulong? groupId = null,
    string? groupName = null);

    Task<AssignResult> RemoveAsync(
        ulong assignmentId,
        string staffUserId,
        string adjustmentReason);

    Task<AssignResult> LockAsync(
        ulong assignmentId,
        string staffUserId,
        string? adjustmentReason = null);

    Task<AssignResult> UnlockAsync(
        ulong assignmentId,
        string staffUserId,
        string? adjustmentReason = null);
}
