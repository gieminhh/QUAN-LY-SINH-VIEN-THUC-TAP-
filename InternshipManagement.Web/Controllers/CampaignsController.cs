using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels.Campaigns;

namespace InternshipManagement.Web.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.TrainingManager},{AppRoles.FacultyManager}")]
public class CampaignsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserScopeService _scopeService;
    private readonly ILogger<CampaignsController> _logger;

    public CampaignsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IUserScopeService scopeService,
        ILogger<CampaignsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _scopeService = scopeService;
        _logger = logger;
    }

    // 1. Danh sách đợt thực tập / đồ án (US-05)
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? kindFilter,
        string? statusFilter,
        ulong? termFilter,
        ulong? majorFilter,
        int page = 1)
    {
        int pageSize = 10;
        var currentUserId = _userManager.GetUserId(User)!;

        // Base query with includes
        var baseQuery = _context.Campaigns
            .Include(c => c.Term)
                .ThenInclude(t => t!.AcademicYear)
            .Include(c => c.Major)
                .ThenInclude(m => m!.Faculty)
            .Include(c => c.CreatedByUser)
            .AsQueryable();

        // Check backend permission filter through user's scopes (US-03)
        var accessibleQuery = await _scopeService.FilterAccessibleCampaignsAsync(currentUserId, baseQuery);

        // Apply Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            accessibleQuery = accessibleQuery.Where(c => c.Code.Contains(s) || c.Name.Contains(s));
        }

        // Apply Kind Filter
        if (!string.IsNullOrWhiteSpace(kindFilter) && kindFilter != "ALL")
        {
            accessibleQuery = accessibleQuery.Where(c => c.Kind == kindFilter);
        }

        // Apply Status Filter
        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "ALL")
        {
            accessibleQuery = accessibleQuery.Where(c => c.Status == statusFilter);
        }

        // Apply Term Filter
        if (termFilter.HasValue && termFilter > 0)
        {
            accessibleQuery = accessibleQuery.Where(c => c.TermId == termFilter.Value);
        }

        // Apply Major Filter
        if (majorFilter.HasValue && majorFilter > 0)
        {
            accessibleQuery = accessibleQuery.Where(c => c.MajorId == majorFilter.Value);
        }

        var totalCount = await accessibleQuery.CountAsync();

        var items = await accessibleQuery
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var campaignItems = new List<CampaignItemViewModel>();
        foreach (var c in items)
        {
            var canManage = await _scopeService.CanManageCampaignAsync(currentUserId, c.Id);
            campaignItems.Add(new CampaignItemViewModel
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                Kind = c.Kind,
                MajorName = c.Major?.Name ?? "N/A",
                FacultyName = c.Major?.Faculty?.Name ?? "N/A",
                TermDisplayName = c.Term != null ? $"HK{c.Term.TermNo} ({c.Term.AcademicYear?.Code})" : "N/A",
                TrainingSystem = c.TrainingSystem,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Status = c.Status,
                CreatedByUserName = c.CreatedByUser?.FullName ?? c.CreatedByUserId,
                CanEdit = canManage && c.Status != CampaignStatuses.Archived,
                CanChangeStatus = canManage
            });
        }

        // Populate dropdown filter options
        var terms = await _context.Terms
            .Include(t => t.AcademicYear)
            .OrderByDescending(t => t.StartDate)
            .Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = $"HK{t.TermNo} ({t.AcademicYear!.Code})"
            })
            .ToListAsync();

        var manageableMajors = await _scopeService.FilterManageableMajorsAsync(currentUserId, _context.Majors);
        var scopedCampaigns = await _scopeService.FilterAccessibleCampaignsAsync(currentUserId, _context.Campaigns);
        var filterMajors = manageableMajors.Union(_context.Majors.Where(m => scopedCampaigns.Any(c => c.MajorId == m.Id)));
        var majors = await filterMajors
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .Select(m => new SelectListItem
            {
                Value = m.Id.ToString(),
                Text = $"{m.Code} - {m.Name}"
            })
            .ToListAsync();

        var viewModel = new CampaignListViewModel
        {
            Campaigns = campaignItems,
            Search = search,
            KindFilter = kindFilter,
            StatusFilter = statusFilter,
            TermFilter = termFilter,
            MajorFilter = majorFilter,
            TermOptions = terms,
            MajorOptions = majors,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return View(viewModel);
    }

    // 2. Tạo đợt mới (US-05)
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CampaignCreateEditViewModel
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(3)
        };
        await PopulateDropdowns(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CampaignCreateEditViewModel model)
    {
        var currentUserId = _userManager.GetUserId(User)!;

        // Backend validation: check if user has permission to manage this major
        if (!await _scopeService.CanManageMajorAsync(currentUserId, model.MajorId))
        {
            ModelState.AddModelError(nameof(model.MajorId), "Bạn không có quyền quản lý đợt cho ngành đào tạo này.");
        }

        // Check duplicate campaign code
        if (await _context.Campaigns.AnyAsync(c => c.Code == model.Code))
        {
            ModelState.AddModelError(nameof(model.Code), "Mã đợt này đã tồn tại trong hệ thống. Vui lòng chọn mã khác.");
        }
        await ValidateCampaignPeriodAsync(model);
        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(model);
            return View(model);
        }

        var campaign = new Campaign
        {
            Code = model.Code.Trim(),
            Name = model.Name.Trim(),
            TermId = model.TermId,
            MajorId = model.MajorId,
            Kind = model.Kind,
            TrainingSystem = (model.TrainingSystem.Trim() == "Chính quy" ? "CHINH_QUY" : model.TrainingSystem.Trim()),
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            Status = CampaignStatuses.Draft,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Campaigns.Add(campaign);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Tạo đợt '{campaign.Name}' thành công!";
        return RedirectToAction(nameof(Index));
    }

    // 3. Chỉnh sửa đợt (US-05)
    [HttpGet]
    public async Task<IActionResult> Edit(ulong id)
    {
        var currentUserId = _userManager.GetUserId(User)!;
        if (!await _scopeService.CanManageCampaignAsync(currentUserId, id))
        {
            return Forbid();
        }

        var campaign = await _context.Campaigns.FindAsync(id);
        if (campaign == null)
        {
            return NotFound();
        }

        if (campaign.Status == CampaignStatuses.Archived)
        {
            TempData["ErrorMessage"] = "Không thể chỉnh sửa đợt đã lưu trữ (ARCHIVED).";
            return RedirectToAction(nameof(Index));
        }

        var model = new CampaignCreateEditViewModel
        {
            Id = campaign.Id,
            Code = campaign.Code,
            Name = campaign.Name,
            TermId = campaign.TermId,
            MajorId = campaign.MajorId,
            Kind = campaign.Kind,
            TrainingSystem = campaign.TrainingSystem,
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            Status = campaign.Status
        };

        await PopulateDropdowns(model, campaign.MajorId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CampaignCreateEditViewModel model)
    {
        var currentUserId = _userManager.GetUserId(User)!;
        if (!await _scopeService.CanManageCampaignAsync(currentUserId, model.Id))
        {
            return Forbid();
        }

        var campaign = await _context.Campaigns.FindAsync(model.Id);
        if (campaign == null)
        {
            return NotFound();
        }

        // Check duplicate code if changed
        if (model.Code != campaign.Code)
        {
            if (await _context.Campaigns.AnyAsync(c => c.Code == model.Code && c.Id != model.Id))
            {
                ModelState.AddModelError(nameof(model.Code), "Mã đợt này đã được sử dụng.");
            }
        }

        // Check scope for major if changed
        if (model.MajorId != campaign.MajorId)
        {
            if (!await _scopeService.CanManageMajorAsync(currentUserId, model.MajorId))
            {
                ModelState.AddModelError(nameof(model.MajorId), "Bạn không có quyền chuyển đợt sang ngành này.");
            }
        }
        await ValidateCampaignPeriodAsync(model);

        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(model, campaign.MajorId);
            return View(model);
        }

        campaign.Code = model.Code.Trim();
        campaign.Name = model.Name.Trim();
        campaign.TermId = model.TermId;
        campaign.MajorId = model.MajorId;
        campaign.Kind = model.Kind;
        campaign.TrainingSystem = (model.TrainingSystem.Trim() == "Chính quy" ? "CHINH_QUY" : model.TrainingSystem.Trim());
        campaign.StartDate = model.StartDate;
        campaign.EndDate = model.EndDate;
        campaign.Status = model.Status;
        campaign.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Cập nhật đợt '{campaign.Name}' thành công!";
        return RedirectToAction(nameof(Index));
    }

    // 4. Chi tiết đợt (US-05)
    [HttpGet]
    public async Task<IActionResult> Details(ulong id)
    {
        var currentUserId = _userManager.GetUserId(User)!;

        var campaign = await _context.Campaigns
            .Include(c => c.Term)
                .ThenInclude(t => t!.AcademicYear)
            .Include(c => c.Major)
                .ThenInclude(m => m!.Faculty)
            .Include(c => c.CreatedByUser)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (campaign == null)
        {
            return NotFound();
        }

        // Check if user has scope to access this campaign
        if (!await _scopeService.CanManageCampaignAsync(currentUserId, id))
        {
            return Forbid();
        }

        var model = new CampaignDetailsViewModel
        {
            Id = campaign.Id,
            Code = campaign.Code,
            Name = campaign.Name,
            Kind = campaign.Kind,
            MajorName = campaign.Major?.Name ?? "N/A",
            MajorCode = campaign.Major?.Code ?? "N/A",
            FacultyName = campaign.Major?.Faculty?.Name ?? "N/A",
            TermName = campaign.Term != null ? $"Học kỳ {campaign.Term.TermNo}" : "N/A",
            AcademicYear = campaign.Term?.AcademicYear?.Code ?? "N/A",
            TrainingSystem = campaign.TrainingSystem,
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            Status = campaign.Status,
            CreatedByUserName = campaign.CreatedByUser?.UserName ?? "N/A",
            CreatedByFullName = campaign.CreatedByUser?.FullName ?? "N/A",
            CreatedAt = campaign.CreatedAt,
            UpdatedAt = campaign.UpdatedAt,
            CanEdit = campaign.Status != CampaignStatuses.Archived,
            CanChangeStatus = true
        };

        return View(model);
    }

    // 5. Chuyển trạng thái mở / đóng đợt (US-05)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(ulong id, string newStatus)
    {
        var currentUserId = _userManager.GetUserId(User)!;
        if (!await _scopeService.CanManageCampaignAsync(currentUserId, id))
        {
            return Forbid();
        }

        var campaign = await _context.Campaigns.FindAsync(id);
        if (campaign == null)
        {
            return NotFound();
        }

        var validStatuses = new[] { CampaignStatuses.Draft, CampaignStatuses.Open, CampaignStatuses.Closed, CampaignStatuses.Archived };
        if (!validStatuses.Contains(newStatus))
        {
            TempData["ErrorMessage"] = "Trạng thái mới không hợp lệ.";
            return RedirectToAction(nameof(Details), new { id });
        }

        campaign.Status = newStatus;
        campaign.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var statusName = newStatus switch
        {
            CampaignStatuses.Draft => "Bản nháp",
            CampaignStatuses.Open => "Đang mở",
            CampaignStatuses.Closed => "Đã đóng",
            CampaignStatuses.Archived => "Lưu trữ",
            _ => newStatus
        };

        TempData["SuccessMessage"] = $"Đã chuyển trạng thái đợt '{campaign.Code}' sang: {statusName}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateDropdowns(CampaignCreateEditViewModel model, ulong? existingMajorId = null)
    {
        model.TermOptions = await _context.Terms
            .Include(t => t.AcademicYear)
            .OrderByDescending(t => t.StartDate)
            .Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = $"Học kỳ {t.TermNo} — {t.AcademicYear!.Code}"
            })
            .ToListAsync();

        var currentUserId = _userManager.GetUserId(User)!;
        var manageableMajors = await _scopeService.FilterManageableMajorsAsync(currentUserId, _context.Majors);
        // Keep the saved major when editing a campaign-scoped assignment, including inactive majors.
        var selectableMajors = _context.Majors.Where(m =>
            (m.IsActive && manageableMajors.Any(allowed => allowed.Id == m.Id)) || m.Id == existingMajorId);
        model.MajorOptions = await selectableMajors
            .Include(m => m.Faculty)
            .OrderBy(m => m.Faculty!.Name).ThenBy(m => m.Name)
            .Select(m => new SelectListItem
            {
                Value = m.Id.ToString(),
                Text = $"{m.Name} ({m.Code}) - {m.Faculty!.Name}"
            })
            .ToListAsync();

        model.KindOptions = new List<SelectListItem>
        {
            new() { Value = CampaignKinds.Project, Text = "Đồ án tốt nghiệp" },
            new() { Value = CampaignKinds.Internship, Text = "Thực tập doanh nghiệp" }
        };

        model.StatusOptions = new List<SelectListItem>
        {
            new() { Value = CampaignStatuses.Draft, Text = "Bản nháp" },
            new() { Value = CampaignStatuses.Open, Text = "Đang mở" },
            new() { Value = CampaignStatuses.Closed, Text = "Đã đóng" },
            new() { Value = CampaignStatuses.Archived, Text = "Lưu trữ" }
        };
    }
    private async Task ValidateCampaignPeriodAsync(CampaignCreateEditViewModel model)
    {
        var term = await _context.Terms
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == model.TermId);

        if (term == null)
        {
            ModelState.AddModelError(
                nameof(model.TermId),
                "Học kỳ được chọn không tồn tại."
            );
            return;
        }

        if (model.StartDate.Date < term.StartDate.Date)
        {
            ModelState.AddModelError(
                nameof(model.StartDate),
                "Ngày bắt đầu đợt phải nằm trong thời gian học kỳ."
            );
        }

        if (model.EndDate.Date > term.EndDate.Date)
        {
            ModelState.AddModelError(
                nameof(model.EndDate),
                "Ngày kết thúc đợt phải nằm trong thời gian học kỳ."
            );
        }
    }
}
