using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels.TopicApprovals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Web.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.TrainingManager},{AppRoles.FacultyManager}")]
public class TopicApprovalsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserScopeService _scopeService;
    private readonly IWebHostEnvironment _environment;

    public TopicApprovalsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IUserScopeService scopeService,
        IWebHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _scopeService = scopeService;
        _environment = environment;
    }

    // DANH SÁCH VÀ LỊCH SỬ DUYỆT ĐỀ TÀI
    [HttpGet]
    public async Task<IActionResult> Index(TopicApprovalListViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null || !user.IsActive)
            return Forbid();

        var reviewableQuery =
            await _scopeService.FilterReviewableCampaignsAsync(
                user.Id,
                _context.Campaigns
                    .AsNoTracking()
                    .Include(c => c.Major));

        var campaigns = await reviewableQuery.ToListAsync();

        var campaignIds = campaigns.Select(c => c.Id).ToList();

        var query = _context.Topics
            .AsNoTracking()
            .Include(t => t.Campaign)
            .Include(t => t.CreatedByUser)
            .Where(t => campaignIds.Contains(t.CampaignId));

        // Tìm kiếm theo tên đề tài
        if (!string.IsNullOrWhiteSpace(model.Search))
        {
            var search = model.Search.Trim();
            query = query.Where(t => t.Title.Contains(search));
        }

        // Lọc theo đợt đào tạo
        if (model.CampaignFilter.HasValue)
        {
            query = query.Where(t =>
                t.CampaignId == model.CampaignFilter.Value);
        }

        // Lọc theo trạng thái duyệt
        var validStatuses = new[]
        {
            TopicApprovalStatuses.Pending,
            TopicApprovalStatuses.Approved,
            TopicApprovalStatuses.Rejected
        };

        if (validStatuses.Contains(model.ApprovalFilter))
        {
            query = query.Where(t =>
                t.ApprovalStatus == model.ApprovalFilter);
        }
        else
        {
            model.ApprovalFilter = "ALL";
        }

        model.TotalCount = await query.CountAsync();
        model.PageSize = 10;

        if (model.Page < 1)
            model.Page = 1;

        if (model.TotalPages > 0 && model.Page > model.TotalPages)
            model.Page = model.TotalPages;

        model.Topics = await query
            .OrderByDescending(t => t.ReviewedAt ?? t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Skip((model.Page - 1) * model.PageSize)
            .Take(model.PageSize)
            .Select(t => new TopicApprovalListItemViewModel
            {
                Id = t.Id,
                CampaignId = t.CampaignId,
                CampaignName = t.Campaign != null
                    ? t.Campaign.Name
                    : "Không xác định",
                Title = t.Title,
                TopicType = t.TopicType,
                Status = t.Status,
                ApprovalStatus = t.ApprovalStatus,
                CreatedByName = t.CreatedByUser != null
                    ? t.CreatedByUser.FullName ?? t.CreatedByUser.UserName ?? "Không xác định"
                    : "Không xác định",
                CreatedAt = t.CreatedAt,
                ReviewedByUserId = t.ReviewedByUserId,
                ReviewedAt = t.ReviewedAt,
                RejectionReason = t.RejectionReason
            })
            .ToListAsync();

        // Lấy tên người duyệt
        var reviewerIds = model.Topics
            .Where(t => !string.IsNullOrWhiteSpace(t.ReviewedByUserId))
            .Select(t => t.ReviewedByUserId!)
            .Distinct()
            .ToList();

        if (reviewerIds.Count > 0)
        {
            var reviewers = await _context.Users
                .AsNoTracking()
                .Where(u => reviewerIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.UserName
                })
                .ToDictionaryAsync(
                    u => u.Id,
                    u => !string.IsNullOrWhiteSpace(u.FullName)
                        ? u.FullName
                        : u.UserName ?? "Không xác định");

            foreach (var topic in model.Topics)
            {
                if (topic.ReviewedByUserId != null &&
                    reviewers.TryGetValue(
                        topic.ReviewedByUserId,
                        out var reviewerName))
                {
                    topic.ReviewerName = reviewerName;
                }
            }
        }

        model.CampaignOptions = campaigns
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
                Selected = model.CampaignFilter == c.Id
            })
            .ToList();

        return View(model);
    }

    // XEM CHI TIẾT ĐỀ TÀI
    [HttpGet]
    public async Task<IActionResult> Details(ulong id)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null || !user.IsActive)
            return Forbid();

        var topic = await _context.Topics
            .AsNoTracking()
            .Include(t => t.Campaign)
            .Include(t => t.Major)
            .Include(t => t.CreatedByUser)
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (topic == null)
            return NotFound();

        if (!await _scopeService.CanReviewCampaignAsync(
            user.Id, topic.CampaignId))
        {
            return Forbid();
        }

        if (!string.IsNullOrWhiteSpace(topic.ReviewedByUserId))
        {
            var reviewer = await _userManager.FindByIdAsync(
                topic.ReviewedByUserId);

            ViewBag.ReviewerName =
                reviewer?.FullName ?? reviewer?.UserName ?? "Không xác định";
        }

        ViewBag.IsSelfCreated = topic.CreatedByUserId == user.Id;

        return View(topic);
    }

    // DUYỆT ĐỀ TÀI
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(ulong id)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null || !user.IsActive)
            return Forbid();

        var topic = await _context.Topics
            .FirstOrDefaultAsync(t => t.Id == id);

        if (topic == null)
            return NotFound();

        if (!await _scopeService.CanReviewCampaignAsync(
            user.Id, topic.CampaignId))
        {
            return Forbid();
        }

        if (topic.CreatedByUserId == user.Id)
        {
            TempData["ErrorMessage"] =
                "Bạn không thể tự phê duyệt đề tài do chính mình tạo.";

            return RedirectToAction(nameof(Details), new { id });
        }

        if (topic.ApprovalStatus != TopicApprovalStatuses.Pending)
        {
            TempData["ErrorMessage"] =
                "Đề tài không còn ở trạng thái chờ duyệt.";

            return RedirectToAction(nameof(Index));
        }

        var now = DateTime.UtcNow;

        topic.ApprovalStatus = TopicApprovalStatuses.Approved;
        topic.ReviewedByUserId = user.Id;
        topic.ReviewedAt = now;
        topic.RejectionReason = null;
        topic.UpdatedAt = now;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã duyệt đề tài thành công.";

        return RedirectToAction(nameof(Index));
    }

    // TỪ CHỐI ĐỀ TÀI
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(
        ulong id,
        string? rejectionReason)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null || !user.IsActive)
            return Forbid();

        var topic = await _context.Topics
            .FirstOrDefaultAsync(t => t.Id == id);

        if (topic == null)
            return NotFound();

        if (!await _scopeService.CanReviewCampaignAsync(
            user.Id, topic.CampaignId))
        {
            return Forbid();
        }

        if (topic.CreatedByUserId == user.Id)
        {
            TempData["ErrorMessage"] =
                "Bạn không thể tự từ chối đề tài do chính mình tạo.";

            return RedirectToAction(nameof(Details), new { id });
        }

        if (topic.ApprovalStatus != TopicApprovalStatuses.Pending)
        {
            TempData["ErrorMessage"] =
                "Đề tài không còn ở trạng thái chờ duyệt.";

            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối.";
            return RedirectToAction(nameof(Details), new { id });
        }

        rejectionReason = rejectionReason.Trim();

        if (rejectionReason.Length > 2000)
        {
            TempData["ErrorMessage"] =
                "Lý do từ chối không được vượt quá 2000 ký tự.";

            return RedirectToAction(nameof(Details), new { id });
        }

        var now = DateTime.UtcNow;

        topic.ApprovalStatus = TopicApprovalStatuses.Rejected;
        topic.ReviewedByUserId = user.Id;
        topic.ReviewedAt = now;
        topic.RejectionReason = rejectionReason;
        topic.UpdatedAt = now;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã từ chối đề tài.";

        return RedirectToAction(nameof(Index));
    }

    // TẢI TỆP ĐÍNH KÈM TRONG PHẠM VI ĐƯỢC PHÉP DUYỆT
    [HttpGet]
    public async Task<IActionResult> DownloadAttachment(ulong id)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null || !user.IsActive)
            return Forbid();

        var attachment = await _context.Set<TopicAttachment>()
            .AsNoTracking()
            .Include(a => a.Topic)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attachment == null)
            return NotFound();

        var topic = attachment.Topic;

        if (topic == null)
            return NotFound();

        if (!await _scopeService.CanReviewCampaignAsync(
            user.Id, topic.CampaignId))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(attachment.StoredFileName))
            return NotFound();

        var uploadFolder = Path.Combine(
            _environment.WebRootPath
                ?? Path.Combine(_environment.ContentRootPath, "wwwroot"),
            "uploads",
            "topics");

        var filePath = Path.Combine(
            uploadFolder,
            Path.GetFileName(attachment.StoredFileName));

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var contentType = string.IsNullOrWhiteSpace(attachment.ContentType)
            ? "application/octet-stream"
            : attachment.ContentType;

        return PhysicalFile(
            filePath,
            contentType,
            attachment.OriginalFileName);
    }
}