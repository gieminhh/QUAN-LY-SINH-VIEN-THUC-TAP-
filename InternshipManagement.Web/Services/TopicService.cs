using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.ViewModels;

namespace InternshipManagement.Web.Services;

public class TopicService : ITopicService
{
    private readonly ApplicationDbContext _context;
    private readonly CampaignRulesService _rules;
    private readonly IFileStorageService _fileStorage;
    private readonly IUserScopeService _scopeService;

    public TopicService(ApplicationDbContext context, IFileStorageService fileStorage, IUserScopeService scopeService, CampaignRulesService rules)
    {
        _context = context;
        _rules = rules;
        _fileStorage = fileStorage;
        _scopeService = scopeService;
    }

    public async Task<TopicListViewModel> GetLecturerTopicsViewModelAsync(ulong lecturerProfileId, ulong? campaignId, string? status, string? search, int page, int pageSize)
    {
        var query = _context.Topics
            .Include(t => t.Campaign)
                .ThenInclude(c => c!.Term)
                    .ThenInclude(te => te!.AcademicYear)
            .Include(t => t.LecturerProfile)
                .ThenInclude(lp => lp!.User)
            .Where(t => t.LecturerProfileId == lecturerProfileId);

        if (campaignId.HasValue && campaignId.Value > 0)
        {
            query = query.Where(t => t.CampaignId == campaignId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(t => t.Code.Contains(s) || t.Title.Contains(s) || (t.Technologies != null && t.Technologies.Contains(s)));
        }

        var totalItems = await query.CountAsync();
        var topics = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var campaigns = await _context.Campaigns
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new { c.Id, Display = $"{c.Code} - {c.Name}" })
            .ToListAsync();

        var statuses = new List<SelectListItem>
        {
            new("Tất cả trạng thái", ""),
            new(TopicStatuses.GetDisplayName(TopicStatuses.Draft), TopicStatuses.Draft),
            new(TopicStatuses.GetDisplayName(TopicStatuses.Pending), TopicStatuses.Pending),
            new(TopicStatuses.GetDisplayName(TopicStatuses.RevisionRequired), TopicStatuses.RevisionRequired),
            new(TopicStatuses.GetDisplayName(TopicStatuses.Approved), TopicStatuses.Approved),
            new(TopicStatuses.GetDisplayName(TopicStatuses.Rejected), TopicStatuses.Rejected),
        };

        return new TopicListViewModel
        {
            Topics = topics,
            CampaignId = campaignId,
            Status = status,
            SearchQuery = search,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            CampaignOptions = new SelectList(campaigns, "Id", "Display", campaignId),
            StatusOptions = new SelectList(statuses, "Value", "Text", status)
        };
    }

    public async Task<Topic?> GetTopicByIdAsync(ulong topicId)
    {
        return await _context.Topics
            .Include(t => t.Campaign)
                .ThenInclude(c => c!.Term)
                    .ThenInclude(te => te!.AcademicYear)
            .Include(t => t.Campaign)
                .ThenInclude(c => c!.Major)
                    .ThenInclude(m => m!.Faculty)
            .Include(t => t.LecturerProfile)
                .ThenInclude(lp => lp!.User)
            .Include(t => t.LecturerProfile)
                .ThenInclude(lp => lp!.Faculty)
            .Include(t => t.ApprovalHistories)
                .ThenInclude(h => h.ActorUser)
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == topicId);
    }

    public async Task<TopicDetailsViewModel?> GetTopicDetailsViewModelAsync(ulong topicId, string currentUserId, bool isReviewer)
    {
        var topic = await GetTopicByIdAsync(topicId);
        if (topic == null) return null;

        var isOwner = topic.LecturerProfile?.UserId == currentUserId;
        var canEdit = isOwner && (topic.Status == TopicStatuses.Draft || topic.Status == TopicStatuses.RevisionRequired);
        var canSubmit = isOwner && (topic.Status == TopicStatuses.Draft || topic.Status == TopicStatuses.RevisionRequired);
        var canReview = isReviewer && topic.Status == TopicStatuses.Pending;

        var document = topic.Attachments
            .Where(a => a.FileCategory == FileCategories.TopicDocument)
            .OrderByDescending(a => a.UploadedAt)
            .FirstOrDefault();

        return new TopicDetailsViewModel
        {
            Topic = topic,
            Histories = topic.ApprovalHistories.OrderByDescending(h => h.ActionAt).ToList(),
            DocumentAttachment = document,
            CanEdit = canEdit,
            CanSubmit = canSubmit,
            CanReview = canReview
        };
    }

    public async Task<bool> IsCodeUniqueInCampaignAsync(ulong campaignId, string code, ulong? excludeTopicId = null)
    {
        var trimmed = code.Trim();
        var query = _context.Topics.Where(t => t.CampaignId == campaignId && t.Code == trimmed);
        if (excludeTopicId.HasValue && excludeTopicId.Value > 0)
        {
            query = query.Where(t => t.Id != excludeTopicId.Value);
        }
        return !await query.AnyAsync();
    }

    public async Task<(bool Success, string? ErrorMessage, ulong TopicId)> CreateTopicAsync(ulong lecturerProfileId, TopicCreateEditViewModel model, string userId)
    {
        var campaign = await _context.Campaigns.FindAsync(model.CampaignId);
        if (campaign == null)
        {
            return (false, "Đợt thực tập / đồ án không tồn tại.", 0);
        }

        var windowError = await _rules.CheckWindow(model.CampaignId,userId,"REGISTRATION");
        if(windowError!=null)return (false,windowError,0);
        var isUnique = await IsCodeUniqueInCampaignAsync(model.CampaignId, model.Code);
        if (!isUnique)
        {
            return (false, $"Mã đề tài '{model.Code.Trim()}' đã tồn tại trong đợt này. Vui lòng chọn mã khác.", 0);
        }

        var status = model.SubmitImmediately ? TopicStatuses.Pending : TopicStatuses.Draft;

        var topic = new Topic
        {
            CampaignId = model.CampaignId,
            LecturerProfileId = lecturerProfileId,
            Code = model.Code.Trim().ToUpperInvariant(),
            Title = model.Title.Trim(),
            Description = model.Description?.Trim(),
            Objectives = model.Objectives?.Trim(),
            KnowledgeRequirements = model.KnowledgeRequirements?.Trim(),
            Technologies = model.Technologies?.Trim(),
            ResearchField = model.ResearchField?.Trim(),
            MaxStudents = model.MaxStudents,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Topics.Add(topic);
        await _context.SaveChangesAsync();

        // If submitted immediately, write to approval history
        if (model.SubmitImmediately)
        {
            var history = new TopicApprovalHistory
            {
                TopicId = topic.Id,
                ActorUserId = userId,
                ActionType = TopicActionTypes.Submit,
                FromStatus = TopicStatuses.Draft,
                ToStatus = TopicStatuses.Pending,
                CommentText = "Giảng viên gửi xét duyệt đề tài mới",
                ActionAt = DateTime.UtcNow
            };
            _context.TopicApprovalHistories.Add(history);
        }

        // Handle document file upload if provided
        if (model.DocumentFile != null && model.DocumentFile.Length > 0)
        {
            var saveResult = await _fileStorage.SaveFileAsync(model.DocumentFile, "topic_documents");
            if (saveResult.Success)
            {
                var attachment = new FileAttachment
                {
                    TopicId = topic.Id,
                    FileCategory = FileCategories.TopicDocument,
                    OriginalFileName = model.DocumentFile.FileName,
                    StorageKey = saveResult.StorageKey!,
                    ContentType = model.DocumentFile.ContentType ?? "application/octet-stream",
                    SizeBytes = (ulong)model.DocumentFile.Length,
                    UploadedByUserId = userId,
                    UploadedAt = DateTime.UtcNow
                };
                _context.FileAttachments.Add(attachment);
            }
        }

        await _context.SaveChangesAsync();
        return (true, null, topic.Id);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateTopicAsync(ulong topicId, ulong lecturerProfileId, TopicCreateEditViewModel model, string userId)
    {
        var topic = await _context.Topics
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == topicId && t.LecturerProfileId == lecturerProfileId);

        if (topic == null)
        {
            return (false, "Không tìm thấy đề tài hoặc bạn không có quyền chỉnh sửa đề tài này.");
        }

        // Rule: Only DRAFT or REVISION_REQUIRED can be edited directly
        if (topic.Status != TopicStatuses.Draft && topic.Status != TopicStatuses.RevisionRequired)
        {
            return (false, "Đề tài đang ở trạng thái không cho phép chỉnh sửa nội dung.");
        }

        var windowError = await _rules.CheckWindow(model.CampaignId,userId,"REGISTRATION");
        if(windowError!=null)return (false,windowError);
        var isUnique = await IsCodeUniqueInCampaignAsync(model.CampaignId, model.Code, topicId);
        if (!isUnique)
        {
            return (false, $"Mã đề tài '{model.Code.Trim()}' đã tồn tại trong đợt này. Vui lòng chọn mã khác.");
        }

        var oldStatus = topic.Status;

        topic.CampaignId = model.CampaignId;
        topic.Code = model.Code.Trim().ToUpperInvariant();
        topic.Title = model.Title.Trim();
        topic.Description = model.Description?.Trim();
        topic.Objectives = model.Objectives?.Trim();
        topic.KnowledgeRequirements = model.KnowledgeRequirements?.Trim();
        topic.Technologies = model.Technologies?.Trim();
        topic.ResearchField = model.ResearchField?.Trim();
        topic.MaxStudents = model.MaxStudents;
        topic.UpdatedAt = DateTime.UtcNow;

        if (model.SubmitImmediately)
        {
            topic.Status = TopicStatuses.Pending;
            var actionType = oldStatus == TopicStatuses.RevisionRequired ? TopicActionTypes.Resubmit : TopicActionTypes.Submit;
            var history = new TopicApprovalHistory
            {
                TopicId = topic.Id,
                ActorUserId = userId,
                ActionType = actionType,
                FromStatus = oldStatus,
                ToStatus = TopicStatuses.Pending,
                CommentText = actionType == TopicActionTypes.Resubmit ? "Giảng viên cập nhật và gửi lại xét duyệt" : "Giảng viên gửi xét duyệt",
                ActionAt = DateTime.UtcNow
            };
            _context.TopicApprovalHistories.Add(history);
        }

        // Handle document file upload if provided
        if (model.DocumentFile != null && model.DocumentFile.Length > 0)
        {
            var saveResult = await _fileStorage.SaveFileAsync(model.DocumentFile, "topic_documents");
            if (saveResult.Success)
            {
                // Remove previous document
                var prevDoc = topic.Attachments.FirstOrDefault(a => a.FileCategory == FileCategories.TopicDocument);
                if (prevDoc != null)
                {
                    _fileStorage.DeleteFile(prevDoc.StorageKey);
                    _context.FileAttachments.Remove(prevDoc);
                }

                var attachment = new FileAttachment
                {
                    TopicId = topic.Id,
                    FileCategory = FileCategories.TopicDocument,
                    OriginalFileName = model.DocumentFile.FileName,
                    StorageKey = saveResult.StorageKey!,
                    ContentType = model.DocumentFile.ContentType ?? "application/octet-stream",
                    SizeBytes = (ulong)model.DocumentFile.Length,
                    UploadedByUserId = userId,
                    UploadedAt = DateTime.UtcNow
                };
                _context.FileAttachments.Add(attachment);
            }
        }

        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> SubmitTopicForApprovalAsync(ulong topicId, ulong lecturerProfileId, string userId)
    {
        var topic = await _context.Topics
            .FirstOrDefaultAsync(t => t.Id == topicId && t.LecturerProfileId == lecturerProfileId);

        if (topic == null)
        {
            return (false, "Không tìm thấy đề tài hoặc bạn không phải người tạo.");
        }

        if (topic.Status != TopicStatuses.Draft && topic.Status != TopicStatuses.RevisionRequired)
        {
            return (false, "Chỉ có thể gửi xét duyệt các đề tài đang ở trạng thái Bản nháp hoặc Yêu cầu chỉnh sửa.");
        }

        var windowError = await _rules.CheckWindow(topic.CampaignId,userId,"REGISTRATION");
        if(windowError!=null)return (false,windowError);
        var oldStatus = topic.Status;
        var actionType = oldStatus == TopicStatuses.RevisionRequired ? TopicActionTypes.Resubmit : TopicActionTypes.Submit;

        topic.Status = TopicStatuses.Pending;
        topic.UpdatedAt = DateTime.UtcNow;

        var history = new TopicApprovalHistory
        {
            TopicId = topic.Id,
            ActorUserId = userId,
            ActionType = actionType,
            FromStatus = oldStatus,
            ToStatus = TopicStatuses.Pending,
            CommentText = actionType == TopicActionTypes.Resubmit ? "Giảng viên hoàn thiện và gửi lại xét duyệt" : "Giảng viên gửi xét duyệt",
            ActionAt = DateTime.UtcNow
        };

        _context.TopicApprovalHistories.Add(history);
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<TopicListViewModel> GetApprovedTopicsForStudentsViewModelAsync(ulong? campaignId, ulong? majorId, string? search, int page, int pageSize)
    {
        // Students ONLY see APPROVED topics!
        var query = _context.Topics
            .Include(t => t.Campaign)
                .ThenInclude(c => c!.Term)
                    .ThenInclude(te => te!.AcademicYear)
            .Include(t => t.Campaign)
                .ThenInclude(c => c!.Major)
                    .ThenInclude(m => m!.Faculty)
            .Include(t => t.LecturerProfile)
                .ThenInclude(lp => lp!.User)
            .Where(t => t.Status == TopicStatuses.Approved);

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
            query = query.Where(t => t.Code.Contains(s) || t.Title.Contains(s) || (t.Technologies != null && t.Technologies.Contains(s)) || (t.LecturerProfile != null && t.LecturerProfile.User != null && t.LecturerProfile.User.FullName.Contains(s)));
        }

        var totalItems = await query.CountAsync();
        var topics = await query
            .OrderByDescending(t => t.ApprovedAt ?? t.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var campaigns = await _context.Campaigns
            .Where(c => c.Status == CampaignStatuses.Open || c.Status == CampaignStatuses.Closed)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new { c.Id, Display = $"{c.Code} - {c.Name}" })
            .ToListAsync();

        return new TopicListViewModel
        {
            Topics = topics,
            CampaignId = campaignId,
            SearchQuery = search,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            CampaignOptions = new SelectList(campaigns, "Id", "Display", campaignId)
        };
    }

    public async Task<FileAttachment?> GetTopicAttachmentAsync(ulong attachmentId)
    {
        return await _context.FileAttachments
            .Include(a => a.Topic)
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.FileCategory == FileCategories.TopicDocument);
    }
}
