using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels.InternshipRegistrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Web.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.TrainingManager},{AppRoles.FacultyManager}")]
public class InternshipApprovalsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserScopeService _scopeService;
    private readonly IWebHostEnvironment _environment;

    public InternshipApprovalsController(
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

    [HttpGet]
    public async Task<IActionResult> Index(InternshipApprovalListViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !user.IsActive)
            return Forbid();

        var reviewableCampaignsQuery = await _scopeService.FilterReviewableCampaignsAsync(
            user.Id,
            _context.Campaigns
                .AsNoTracking()
                .Include(c => c.Major)
                .Where(c => c.Kind == CampaignKinds.Internship));

        var campaigns = await reviewableCampaignsQuery.OrderBy(c => c.Name).ToListAsync();
        var campaignIds = campaigns.Select(c => c.Id).ToList();

        var query = _context.InternshipRegistrations
            .AsNoTracking()
            .Include(r => r.Campaign)
            .Include(r => r.StudentUser)
            .Include(r => r.InternshipPlace)
            .Include(r => r.ReviewedByUser)
            .Where(r => campaignIds.Contains(r.CampaignId));

        if (!string.IsNullOrWhiteSpace(model.Search))
        {
            var search = model.Search.Trim();
            query = query.Where(r =>
                (r.StudentUser != null && r.StudentUser.FullName != null && r.StudentUser.FullName.Contains(search))
                || (r.StudentUser != null && r.StudentUser.UserName != null && r.StudentUser.UserName.Contains(search))
                || (r.InternshipPlace != null && r.InternshipPlace.Name.Contains(search)));
        }

        if (model.CampaignFilter.HasValue)
        {
            query = query.Where(r => r.CampaignId == model.CampaignFilter.Value);
        }

        var validStatuses = new[]
        {
            InternshipRegistrationStatuses.Pending,
            InternshipRegistrationStatuses.Approved,
            InternshipRegistrationStatuses.Rejected
        };

        if (!string.IsNullOrWhiteSpace(model.ApprovalFilter) && validStatuses.Contains(model.ApprovalFilter))
        {
            query = query.Where(r => r.ApprovalStatus == model.ApprovalFilter);
        }
        else
        {
            model.ApprovalFilter = "ALL";
        }

        model.PageSize = 10;
        model.TotalCount = await query.CountAsync();

        if (model.Page < 1)
            model.Page = 1;

        if (model.TotalPages > 0 && model.Page > model.TotalPages)
            model.Page = model.TotalPages;

        model.Registrations = await query
            .OrderByDescending(r => r.SubmittedAt)
            .ThenByDescending(r => r.Id)
            .Skip((model.Page - 1) * model.PageSize)
            .Take(model.PageSize)
            .Select(r => new InternshipApprovalListItemViewModel
            {
                Id = r.Id,
                CampaignId = r.CampaignId,
                CampaignName = r.Campaign != null ? r.Campaign.Name : "Không xác định",
                StudentName = r.StudentUser != null
                    ? (r.StudentUser.FullName ?? r.StudentUser.UserName ?? "Không xác định")
                    : "Không xác định",
                PlaceName = r.InternshipPlace != null ? r.InternshipPlace.Name : "Không xác định",
                PositionTitle = r.PositionTitle,
                ApprovalStatus = r.ApprovalStatus,
                RejectionReason = r.RejectionReason,
                SubmittedAt = r.SubmittedAt,
                ReviewerName = r.ReviewedByUser != null
                    ? (r.ReviewedByUser.FullName ?? r.ReviewedByUser.UserName)
                    : null,
                ReviewedAt = r.ReviewedAt
            })
            .ToListAsync();

        model.CampaignOptions = campaigns.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = c.Name,
            Selected = model.CampaignFilter == c.Id
        }).ToList();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(ulong id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !user.IsActive)
            return Forbid();

        var registration = await _context.InternshipRegistrations
            .AsNoTracking()
            .Include(r => r.Campaign)
                .ThenInclude(c => c!.Term)
            .Include(r => r.Campaign)
                .ThenInclude(c => c!.Major)
            .Include(r => r.InternshipPlace)
            .Include(r => r.StudentUser)
            .Include(r => r.ReviewedByUser)
            .Include(r => r.Reviews)
                .ThenInclude(rv => rv.ActorUser)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (registration == null)
            return NotFound();

        if (!await _scopeService.CanReviewCampaignAsync(user.Id, registration.CampaignId))
            return Forbid();

        var vm = InternshipRegistrationsController.BuildDetailViewModel(registration, isManagerView: true);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(ulong id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !user.IsActive)
            return Forbid();

        var registration = await _context.InternshipRegistrations
            .Include(r => r.InternshipPlace)
            .Include(r => r.Reviews)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (registration == null)
            return NotFound();

        if (!await _scopeService.CanReviewCampaignAsync(user.Id, registration.CampaignId))
            return Forbid();

        if (registration.ApprovalStatus != InternshipRegistrationStatuses.Pending)
        {
            TempData["ErrorMessage"] = "Hồ sơ đăng ký thực tập không còn ở trạng thái chờ duyệt.";
            return RedirectToAction(nameof(Index));
        }

        var now = DateTime.UtcNow;
        var nextAttempt = (registration.Reviews.Any()
            ? registration.Reviews.Max(r => r.AttemptNo)
            : 0) + 1;

        registration.ApprovalStatus = InternshipRegistrationStatuses.Approved;
        registration.RejectionReason = null;
        registration.ReviewedByUserId = user.Id;
        registration.ReviewedAt = now;
        registration.UpdatedAt = now;

        _context.InternshipRegistrationReviews.Add(new InternshipRegistrationReview
        {
            RegistrationId = registration.Id,
            AttemptNo = nextAttempt,
            Action = InternshipReviewActions.Approved,
            Comment = "Đã phê duyệt nơi thực tập.",
            PlaceNameSnapshot = registration.InternshipPlace?.Name,
            PositionSnapshot = registration.PositionTitle,
            ActorUserId = user.Id,
            CreatedAt = now
        });

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã phê duyệt đăng ký nơi thực tập thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(ulong id, string? rejectionReason)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !user.IsActive)
            return Forbid();

        var registration = await _context.InternshipRegistrations
            .Include(r => r.InternshipPlace)
            .Include(r => r.Reviews)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (registration == null)
            return NotFound();

        if (!await _scopeService.CanReviewCampaignAsync(user.Id, registration.CampaignId))
            return Forbid();

        if (registration.ApprovalStatus != InternshipRegistrationStatuses.Pending)
        {
            TempData["ErrorMessage"] = "Hồ sơ đăng ký thực tập không còn ở trạng thái chờ duyệt.";
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
            TempData["ErrorMessage"] = "Lý do từ chối không được vượt quá 2000 ký tự.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var now = DateTime.UtcNow;
        var nextAttempt = (registration.Reviews.Any()
            ? registration.Reviews.Max(r => r.AttemptNo)
            : 0) + 1;

        registration.ApprovalStatus = InternshipRegistrationStatuses.Rejected;
        registration.RejectionReason = rejectionReason;
        registration.ReviewedByUserId = user.Id;
        registration.ReviewedAt = now;
        registration.UpdatedAt = now;

        _context.InternshipRegistrationReviews.Add(new InternshipRegistrationReview
        {
            RegistrationId = registration.Id,
            AttemptNo = nextAttempt,
            Action = InternshipReviewActions.Rejected,
            Comment = rejectionReason,
            PlaceNameSnapshot = registration.InternshipPlace?.Name,
            PositionSnapshot = registration.PositionTitle,
            ActorUserId = user.Id,
            CreatedAt = now
        });

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã từ chối hồ sơ đăng ký nơi thực tập và lưu lý do từ chối.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadFile(ulong id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !user.IsActive)
            return Forbid();

        var registration = await _context.InternshipRegistrations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (registration == null)
            return NotFound();

        if (!await _scopeService.CanReviewCampaignAsync(user.Id, registration.CampaignId))
            return Forbid();

        if (string.IsNullOrWhiteSpace(registration.StoredFileName)
            || string.IsNullOrWhiteSpace(registration.OriginalFileName))
            return NotFound();

        var uploadFolder = Path.Combine(
            _environment.WebRootPath
                ?? Path.Combine(_environment.ContentRootPath, "wwwroot"),
            "uploads",
            "internship-registrations");

        var filePath = Path.Combine(uploadFolder, Path.GetFileName(registration.StoredFileName));
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var contentType = string.IsNullOrWhiteSpace(registration.ContentType)
            ? "application/octet-stream"
            : registration.ContentType;

        return PhysicalFile(filePath, contentType, registration.OriginalFileName);
    }
}