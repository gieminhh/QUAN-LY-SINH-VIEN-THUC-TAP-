using InternshipManagement.Web.Models;
using InternshipManagement.Web.ViewModels;

namespace InternshipManagement.Web.Services;

public interface ITopicService
{
    Task<TopicListViewModel> GetLecturerTopicsViewModelAsync(ulong lecturerProfileId, ulong? campaignId, string? status, string? search, int page, int pageSize);
    Task<Topic?> GetTopicByIdAsync(ulong topicId);
    Task<TopicDetailsViewModel?> GetTopicDetailsViewModelAsync(ulong topicId, string currentUserId, bool isReviewer);
    Task<(bool Success, string? ErrorMessage, ulong TopicId)> CreateTopicAsync(ulong lecturerProfileId, TopicCreateEditViewModel model, string userId);
    Task<(bool Success, string? ErrorMessage)> UpdateTopicAsync(ulong topicId, ulong lecturerProfileId, TopicCreateEditViewModel model, string userId);
    Task<(bool Success, string? ErrorMessage)> SubmitTopicForApprovalAsync(ulong topicId, ulong lecturerProfileId, string userId);
    Task<TopicListViewModel> GetApprovedTopicsForStudentsViewModelAsync(ulong? campaignId, ulong? majorId, string? search, int page, int pageSize);
    Task<bool> IsCodeUniqueInCampaignAsync(ulong campaignId, string code, ulong? excludeTopicId = null);
    Task<FileAttachment?> GetTopicAttachmentAsync(ulong attachmentId);
}
