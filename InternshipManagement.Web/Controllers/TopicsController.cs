using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels;

namespace InternshipManagement.Web.Controllers;

[Authorize]
public class TopicsController : Controller
{
    private readonly ITopicService _topicService;
    private readonly IUserScopeService _scopeService;
    private readonly ILecturerProfileService _lecturerService;
    private readonly IFileStorageService _fileStorage;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TopicsController(
        ITopicService topicService,
        IUserScopeService scopeService,
        ILecturerProfileService lecturerService,
        IFileStorageService fileStorage,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _topicService = topicService;
        _scopeService = scopeService;
        _lecturerService = lecturerService;
        _fileStorage = fileStorage;
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Lecturer},{AppRoles.Admin}")]
    public async Task<IActionResult> Index(ulong? campaignId, string? status, string? search, int page = 1)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();

        var lecturerProfile = await _lecturerService.GetProfileByUserIdAsync(currentUserId);
        if (lecturerProfile == null)
        {
            TempData["ErrorMessage"] = "Bạn chưa có hồ sơ giảng viên trong hệ thống để quản lý đề tài.";
            return RedirectToAction("Index", "Home");
        }

        var model = await _topicService.GetLecturerTopicsViewModelAsync(lecturerProfile.Id, campaignId, status, search, page, 10);
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Approved(ulong? campaignId, ulong? majorId, string? search, int page = 1)
    {
        var model = await _topicService.GetApprovedTopicsForStudentsViewModelAsync(campaignId, majorId, search, page, 12);
        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Lecturer},{AppRoles.Admin}")]
    public async Task<IActionResult> Create()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();

        var lecturerProfile = await _lecturerService.GetProfileByUserIdAsync(currentUserId);
        if (lecturerProfile == null)
        {
            TempData["ErrorMessage"] = "Bạn chưa có hồ sơ giảng viên trong hệ thống để tạo đề tài.";
            return RedirectToAction("Index", "Home");
        }

        var activeCampaigns = await _context.Campaigns
            .Where(c => c.Status == CampaignStatuses.Open)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new { c.Id, Display = $"{c.Code} - {c.Name}" })
            .ToListAsync();

        var model = new TopicCreateEditViewModel
        {
            CampaignOptions = new SelectList(activeCampaigns, "Id", "Display")
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Lecturer},{AppRoles.Admin}")]
    public async Task<IActionResult> Create(TopicCreateEditViewModel model)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();

        var lecturerProfile = await _lecturerService.GetProfileByUserIdAsync(currentUserId);
        if (lecturerProfile == null)
        {
            TempData["ErrorMessage"] = "Bạn chưa có hồ sơ giảng viên trong hệ thống.";
            return RedirectToAction("Index", "Home");
        }

        if (!ModelState.IsValid)
        {
            var campaigns = await _context.Campaigns
                .Where(c => c.Status == CampaignStatuses.Open)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new { c.Id, Display = $"{c.Code} - {c.Name}" })
                .ToListAsync();
            model.CampaignOptions = new SelectList(campaigns, "Id", "Display", model.CampaignId);
            return View(model);
        }

        var result = await _topicService.CreateTopicAsync(lecturerProfile.Id, model, currentUserId);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Đã xảy ra lỗi khi tạo đề tài.");
            var campaigns = await _context.Campaigns
                .Where(c => c.Status == CampaignStatuses.Open)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new { c.Id, Display = $"{c.Code} - {c.Name}" })
                .ToListAsync();
            model.CampaignOptions = new SelectList(campaigns, "Id", "Display", model.CampaignId);
            return View(model);
        }

        TempData["SuccessMessage"] = model.SubmitImmediately
            ? "Tạo đề tài và gửi xét duyệt thành công!"
            : "Đã lưu bản nháp đề tài thành công!";

        return RedirectToAction(nameof(Details), new { id = result.TopicId });
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Lecturer},{AppRoles.Admin}")]
    public async Task<IActionResult> Edit(ulong id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();

        var lecturerProfile = await _lecturerService.GetProfileByUserIdAsync(currentUserId);
        if (lecturerProfile == null) return NotFound();

        var topic = await _topicService.GetTopicByIdAsync(id);
        if (topic == null || topic.LecturerProfileId != lecturerProfile.Id)
        {
            return NotFound("Không tìm thấy đề tài hoặc bạn không có quyền chỉnh sửa.");
        }

        if (topic.Status != TopicStatuses.Draft && topic.Status != TopicStatuses.RevisionRequired)
        {
            TempData["ErrorMessage"] = $"Không thể chỉnh sửa đề tài khi đang ở trạng thái '{TopicStatuses.GetDisplayName(topic.Status)}'.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var campaigns = await _context.Campaigns
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new { c.Id, Display = $"{c.Code} - {c.Name}" })
            .ToListAsync();

        var model = new TopicCreateEditViewModel
        {
            Id = topic.Id,
            CampaignId = topic.CampaignId,
            Code = topic.Code,
            Title = topic.Title,
            Description = topic.Description,
            Objectives = topic.Objectives,
            KnowledgeRequirements = topic.KnowledgeRequirements,
            Technologies = topic.Technologies,
            ResearchField = topic.ResearchField,
            MaxStudents = topic.MaxStudents,
            CurrentStatus = topic.Status,
            ExistingAttachment = topic.Attachments.FirstOrDefault(a => a.FileCategory == FileCategories.TopicDocument),
            CampaignOptions = new SelectList(campaigns, "Id", "Display", topic.CampaignId)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Lecturer},{AppRoles.Admin}")]
    public async Task<IActionResult> Edit(ulong id, TopicCreateEditViewModel model)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();

        var lecturerProfile = await _lecturerService.GetProfileByUserIdAsync(currentUserId);
        if (lecturerProfile == null) return NotFound();

        if (!ModelState.IsValid)
        {
            var campaigns = await _context.Campaigns
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new { c.Id, Display = $"{c.Code} - {c.Name}" })
                .ToListAsync();
            model.CampaignOptions = new SelectList(campaigns, "Id", "Display", model.CampaignId);
            return View(model);
        }

        var result = await _topicService.UpdateTopicAsync(id, lecturerProfile.Id, model, currentUserId);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Lỗi khi cập nhật đề tài.");
            var campaigns = await _context.Campaigns
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new { c.Id, Display = $"{c.Code} - {c.Name}" })
                .ToListAsync();
            model.CampaignOptions = new SelectList(campaigns, "Id", "Display", model.CampaignId);
            return View(model);
        }

        TempData["SuccessMessage"] = model.SubmitImmediately
            ? "Đã cập nhật và gửi lại đề tài để xét duyệt!"
            : "Đã cập nhật nội dung đề tài thành công!";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(ulong id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var isReviewer = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.TrainingManager) || User.IsInRole(AppRoles.FacultyManager);

        var model = await _topicService.GetTopicDetailsViewModelAsync(id, currentUserId, isReviewer);
        if (model == null) return NotFound("Không tìm thấy đề tài.");

        // If user is a student: only allowed to see APPROVED topics
        if (!await CanReadTopicAsync(model.Topic))
        {
            return Forbid();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Lecturer},{AppRoles.Admin}")]
    public async Task<IActionResult> Submit(ulong id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();

        var lecturerProfile = await _lecturerService.GetProfileByUserIdAsync(currentUserId);
        if (lecturerProfile == null) return NotFound();

        var result = await _topicService.SubmitTopicForApprovalAsync(id, lecturerProfile.Id, currentUserId);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = "Gửi đề tài xét duyệt thành công! Trạng thái đề tài chuyển sang 'Chờ xét duyệt'.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDocument(ulong attachmentId)
    {
        var attachment = await _topicService.GetTopicAttachmentAsync(attachmentId);
        if (attachment == null) return NotFound("Tài liệu không tồn tại.");

        // Check if student is trying to download document of an unapproved topic
        if (attachment.Topic == null || !await CanReadTopicAsync(attachment.Topic))
        {
            return Forbid();
        }

        if (!_fileStorage.FileExists(attachment.StorageKey))
        {
            return NotFound("Tệp tin không tìm thấy trên hệ thống lưu trữ.");
        }

        var fullPath = _fileStorage.GetFullPath(attachment.StorageKey);
        return PhysicalFile(fullPath, attachment.ContentType, attachment.OriginalFileName);
    }
    private async Task<bool> CanReadTopicAsync(Topic topic)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (User.IsInRole(AppRoles.Admin)) return true;
        if (User.IsInRole(AppRoles.TrainingManager) || User.IsInRole(AppRoles.FacultyManager))
            return await _scopeService.CanManageCampaignAsync(userId, topic.CampaignId);
        if (topic.Status == TopicStatuses.Approved) return true;
        return User.IsInRole(AppRoles.Lecturer) && await _context.LecturerProfiles
            .AnyAsync(p => p.Id == topic.LecturerProfileId && p.UserId == userId);
    }

}
