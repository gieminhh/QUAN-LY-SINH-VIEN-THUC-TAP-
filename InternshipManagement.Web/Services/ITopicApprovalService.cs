using InternshipManagement.Web.Models;
using InternshipManagement.Web.ViewModels;

namespace InternshipManagement.Web.Services;

public interface ITopicApprovalService
{
    Task<TopicApprovalListViewModel> GetTopicsForApprovalViewModelAsync(string reviewerUserId, ulong? campaignId, string? status, ulong? majorId, string? search, int page, int pageSize);
    Task<bool> CanReviewTopicAsync(string reviewerUserId, ulong topicId);
    Task<TopicReviewViewModel?> GetTopicReviewViewModelAsync(ulong topicId, string reviewerUserId);
    Task<(bool Success, string? ErrorMessage)> ApproveTopicAsync(ulong topicId, string reviewerUserId, string? comments = null);
    Task<(bool Success, string? ErrorMessage)> RequestRevisionAsync(ulong topicId, string reviewerUserId, string comments);
    Task<(bool Success, string? ErrorMessage)> RejectTopicAsync(ulong topicId, string reviewerUserId, string comments);
}
