using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.ViewModels;

namespace InternshipManagement.Web.Services;

public class TopicApprovalService : ITopicApprovalService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserScopeService _scopeService;
    private readonly ITopicService _topicService;

    public TopicApprovalService(ApplicationDbContext context, IUserScopeService scopeService, ITopicService topicService)
    {
        _context = context;
        _scopeService = scopeService;
        _topicService = topicService;
    }

    public async Task<bool> CanReviewTopicAsync(string reviewerUserId, ulong topicId)
    {
        var topic = await _context.Topics
            .Include(t => t.Campaign)
                .ThenInclude(c => c!.Major)
            .FirstOrDefaultAsync(t => t.Id == topicId);

        if (topic == null || topic.Campaign == null) return false;

        // Uses scope service to verify permission over this campaign/major/faculty
        return await _scopeService.CanManageCampaignAsync(reviewerUserId, topic.CampaignId);
    }

    public async Task<TopicApprovalListViewModel> GetTopicsForApprovalViewModelAsync(string reviewerUserId, ulong? campaignId, string? status, ulong? majorId, string? search, int page, int pageSize)
    {
        // 1. Get campaigns accessible to this reviewer
        var accessibleCampaignsQuery = await _scopeService.FilterAccessibleCampaignsAsync(reviewerUserId, _context.Campaigns);
        var accessibleCampaignIds = await accessibleCampaignsQuery.Select(c => c.Id).ToListAsync();

        var query = _context.Topics
            .Include(t => t.Campaign)
                .ThenInclude(c => c!.Term)
                    .ThenInclude(te => te!.AcademicYear)
            .Include(t => t.Campaign)
                .ThenInclude(c => c!.Major)
                    .ThenInclude(m => m!.Faculty)
            .Include(t => t.LecturerProfile)
                .ThenInclude(lp => lp!.User)
            .Where(t => accessibleCampaignIds.Contains(t.CampaignId));

        // Default to PENDING if status not specified or empty
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(t => t.Status == status);
        }

        if (campaignId.HasValue && campaignId.Value > 0)
        {
            query = query.Where(t => t.CampaignId == campaignId.Value);
        }

        if (majorId.HasValue && majorId.Value > 0)
        {
            query = query.Where(t => t.Campaign != null && t.Campaign.MajorId == majorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(t => t.Code.Contains(s) || t.Title.Contains(s) || (t.LecturerProfile != null && t.LecturerProfile.User != null && t.LecturerProfile.User.FullName.Contains(s)));
        }

        var totalItems = await query.CountAsync();
        var topics = await query
            .OrderBy(t => t.Status == TopicStatuses.Pending ? 0 : 1) // Prioritize pending
            .ThenByDescending(t => t.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var campaignList = await accessibleCampaignsQuery
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new { c.Id, Display = $"{c.Code} - {c.Name}" })
            .ToListAsync();

        var statuses = new List<SelectListItem>
        {
            new("Tất cả trạng thái", ""),
            new(TopicStatuses.GetDisplayName(TopicStatuses.Pending), TopicStatuses.Pending),
            new(TopicStatuses.GetDisplayName(TopicStatuses.RevisionRequired), TopicStatuses.RevisionRequired),
            new(TopicStatuses.GetDisplayName(TopicStatuses.Approved), TopicStatuses.Approved),
            new(TopicStatuses.GetDisplayName(TopicStatuses.Rejected), TopicStatuses.Rejected),
            new(TopicStatuses.GetDisplayName(TopicStatuses.Draft), TopicStatuses.Draft)
        };

        var manageableMajors = await _scopeService.FilterManageableMajorsAsync(reviewerUserId, _context.Majors);
        var majors = await _context.Majors
            .Where(m => m.IsActive && (manageableMajors.Any(a => a.Id == m.Id) || accessibleCampaignsQuery.Any(c => c.MajorId == m.Id)))
            .OrderBy(m => m.Name)
            .Select(m => new { m.Id, Display = m.Name })
            .ToListAsync();

        return new TopicApprovalListViewModel
        {
            Topics = topics,
            CampaignId = campaignId,
            Status = status,
            MajorId = majorId,
            SearchQuery = search,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            CampaignOptions = new SelectList(campaignList, "Id", "Display", campaignId),
            StatusOptions = new SelectList(statuses, "Value", "Text", status),
            MajorOptions = new SelectList(majors, "Id", "Display", majorId)
        };
    }

    public async Task<TopicReviewViewModel?> GetTopicReviewViewModelAsync(ulong topicId, string reviewerUserId)
    {
        var topic = await _topicService.GetTopicByIdAsync(topicId);
        if (topic == null) return null;

        var canReview = await CanReviewTopicAsync(reviewerUserId, topicId);

        var document = topic.Attachments
            .Where(a => a.FileCategory == FileCategories.TopicDocument)
            .OrderByDescending(a => a.UploadedAt)
            .FirstOrDefault();

        return new TopicReviewViewModel
        {
            Topic = topic,
            Histories = topic.ApprovalHistories.OrderByDescending(h => h.ActionAt).ToList(),
            DocumentAttachment = document,
            CanReview = canReview
        };
    }

    public async Task<(bool Success, string? ErrorMessage)> ApproveTopicAsync(ulong topicId, string reviewerUserId, string? comments = null)
    {
        var canReview = await CanReviewTopicAsync(reviewerUserId, topicId);
        if (!canReview)
        {
            return (false, "Bạn không có quyền xét duyệt đề tài này (ngoài phạm vi quản lý được phân công).");
        }

        var topic = await _context.Topics.FindAsync(topicId);
        if (topic == null)
        {
            return (false, "Không tìm thấy đề tài.");
        }

        if (topic.Status != TopicStatuses.Pending)
        {
            return (false, $"Chỉ có thể phê duyệt các đề tài đang ở trạng thái 'Chờ xét duyệt' (Hiện tại: {TopicStatuses.GetDisplayName(topic.Status)}).");
        }

        return await SaveDecisionAsync(
            topic, reviewerUserId, TopicActionTypes.Approve, TopicStatuses.Approved,
            string.IsNullOrWhiteSpace(comments) ? "Đã phê duyệt đề tài" : comments.Trim());
    }

    public async Task<(bool Success, string? ErrorMessage)> RequestRevisionAsync(ulong topicId, string reviewerUserId, string comments)
    {
        if (string.IsNullOrWhiteSpace(comments))
        {
            return (false, "Vui lòng nhập lý do hoặc nội dung yêu cầu chỉnh sửa đề tài.");
        }

        var canReview = await CanReviewTopicAsync(reviewerUserId, topicId);
        if (!canReview)
        {
            return (false, "Bạn không có quyền xét duyệt đề tài này (ngoài phạm vi quản lý được phân công).");
        }

        var topic = await _context.Topics.FindAsync(topicId);
        if (topic == null)
        {
            return (false, "Không tìm thấy đề tài.");
        }

        if (topic.Status != TopicStatuses.Pending)
        {
            return (false, $"Chỉ có thể yêu cầu chỉnh sửa các đề tài đang ở trạng thái 'Chờ xét duyệt' (Hiện tại: {TopicStatuses.GetDisplayName(topic.Status)}).");
        }

        return await SaveDecisionAsync(
            topic, reviewerUserId, TopicActionTypes.RequestRevision, TopicStatuses.RevisionRequired,
            comments.Trim());
    }

    public async Task<(bool Success, string? ErrorMessage)> RejectTopicAsync(ulong topicId, string reviewerUserId, string comments)
    {
        if (string.IsNullOrWhiteSpace(comments))
        {
            return (false, "Vui lòng nhập lý do từ chối đề tài.");
        }

        var canReview = await CanReviewTopicAsync(reviewerUserId, topicId);
        if (!canReview)
        {
            return (false, "Bạn không có quyền xét duyệt đề tài này (ngoài phạm vi quản lý được phân công).");
        }

        var topic = await _context.Topics.FindAsync(topicId);
        if (topic == null)
        {
            return (false, "Không tìm thấy đề tài.");
        }

        if (topic.Status != TopicStatuses.Pending)
        {
            return (false, $"Chỉ có thể từ chối các đề tài đang ở trạng thái 'Chờ xét duyệt' (Hiện tại: {TopicStatuses.GetDisplayName(topic.Status)}).");
        }

        return await SaveDecisionAsync(
            topic, reviewerUserId, TopicActionTypes.Reject, TopicStatuses.Rejected,
            comments.Trim());
    }

    private async Task<(bool Success, string? ErrorMessage)> SaveDecisionAsync(
        Topic topic, string reviewerUserId, string action, string status, string comment)
    {
        var now = DateTime.UtcNow;
        var approvedAt = status == TopicStatuses.Approved ? now : topic.ApprovedAt;

        // Write the decision and its history together, only if this submission is still current.
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var affected = await _context.Topics
            .Where(t => t.Id == topic.Id && t.Status == TopicStatuses.Pending && t.UpdatedAt == topic.UpdatedAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.Status, status)
                .SetProperty(t => t.ApprovedAt, approvedAt)
                .SetProperty(t => t.UpdatedAt, now));

        if (affected == 0)
        {
            return (false, "Đề tài đã thay đổi trong lúc xét duyệt. Vui lòng kiểm tra lại trước khi xử lý.");
        }

        _context.TopicApprovalHistories.Add(new TopicApprovalHistory
        {
            TopicId = topic.Id,
            ActorUserId = reviewerUserId,
            ActionType = action,
            FromStatus = TopicStatuses.Pending,
            ToStatus = status,
            CommentText = comment,
            ActionAt = now
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return (true, null);
    }
}
