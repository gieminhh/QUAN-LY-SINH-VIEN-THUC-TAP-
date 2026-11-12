
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.ViewModels.Topics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Web.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Lecturer}")]
public class TopicsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<TopicsController> _logger;

    public TopicsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment,
        ILogger<TopicsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
        _logger = logger;
    }

    
    public async Task<IActionResult> Index(TopicListViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null || !user.IsActive)
            return Forbid();

        var isAdmin = User.IsInRole(AppRoles.Admin);

        var campaigns = await GetAccessibleCampaignsAsync(user);

        if (campaigns == null)
            return Forbid();

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

    // Lọc theo năm học
        if (model.AcademicYearFilter.HasValue)
        {
            query = query.Where(t =>
                t.Campaign!.Term!.AcademicYearId == model.AcademicYearFilter.Value);
        }

    // Lọc theo khoa
        if (model.FacultyFilter.HasValue)
        {
            query = query.Where(t =>
                t.Campaign!.Major!.FacultyId == model.FacultyFilter.Value);
        }

    // Lọc theo giảng viên được phân công
        if (!string.IsNullOrWhiteSpace(model.LecturerFilter))
        {
            query = query.Where(t =>
                t.Lecturers.Any(l =>
                    l.LecturerUserId == model.LecturerFilter));
        }

    // Lọc theo hình thức đề tài
    if (model.TypeFilter != "ALL")
    {
        query = query.Where(t =>
            t.TopicType == model.TypeFilter);
    }

    // Lọc theo trạng thái
    if (model.StatusFilter != "ALL")
    {
        query = query.Where(t =>
            t.Status == model.StatusFilter);
    }

    // Tổng số đề tài sau khi lọc
    model.TotalCount = await query.CountAsync();

    model.PageSize = 10;

    if (model.Page < 1)
        model.Page = 1;

    if (model.TotalPages > 0 && model.Page > model.TotalPages)
        model.Page = model.TotalPages;

    // Lấy danh sách đề tài
        model.Topics = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((model.Page - 1) * model.PageSize)
            .Take(model.PageSize)
            .Select(t => new TopicListItemViewModel
        {
            Id = t.Id,
            CampaignId = t.CampaignId,
            CampaignName = t.Campaign!.Name,
            CampaignKind = t.Campaign!.Kind,
            Title = t.Title,
            TopicType = t.TopicType,
            Status = t.Status,
            MaxStudents = t.MaxStudents,
            MaxGroups = t.MaxGroups,
            CreatedByName = t.CreatedByUser!.FullName,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            CanEdit = isAdmin || t.CreatedByUserId == user.Id
        })
        .ToListAsync();

    // Danh sách đợt đào tạo
    model.CampaignOptions = campaigns
        .Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = c.Name,
            Selected = model.CampaignFilter == c.Id
        })
        .ToList();

    // Danh sách năm học trong phạm vi được phép xem
    model.AcademicYearOptions = await _context.AcademicYears
        .Where(y => _context.Campaigns.Any(c =>
            campaignIds.Contains(c.Id) &&
            c.Term!.AcademicYearId == y.Id))
        .OrderByDescending(y => y.Code)
        .Select(y => new SelectListItem
        {
            Value = y.Id.ToString(),
            Text = y.Code,
            Selected = model.AcademicYearFilter == y.Id
        })
        .ToListAsync();

    // Danh sách khoa trong phạm vi được phép xem
    model.FacultyOptions = await _context.Faculties
        .Where(f => _context.Campaigns.Any(c =>
            campaignIds.Contains(c.Id) &&
            c.Major!.FacultyId == f.Id))
        .OrderBy(f => f.Name)
        .Select(f => new SelectListItem
        {
            Value = f.Id.ToString(),
            Text = f.Name,
            Selected = model.FacultyFilter == f.Id
        })
        .ToListAsync();

    // Danh sách giảng viên được phân công đề tài
    model.LecturerOptions = await _context.TopicLecturers
        .Where(l => campaignIds.Contains(l.Topic!.CampaignId))
        .Select(l => new
        {
            l.LecturerUserId,
            FullName = l.Lecturer!.FullName
        })
        .Distinct()
        .OrderBy(l => l.FullName)
        .Select(l => new SelectListItem
        {
            Value = l.LecturerUserId,
            Text = l.FullName,
            Selected = model.LecturerFilter == l.LecturerUserId
        })
        .ToListAsync();

    return View(model);
}


    // LẤY CÁC ĐỢT ĐÀO TẠO MÀ NGƯỜI DÙNG ĐƯỢC PHÉP TRUY CẬP
    private async Task<List<Campaign>?> GetAccessibleCampaignsAsync(
        ApplicationUser user)
    {
        // Admin được truy cập tất cả đợt đào tạo
        if (User.IsInRole(AppRoles.Admin))
        {
            return await _context.Campaigns
                .AsNoTracking()
                .Include(c => c.Major)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        // Giảng viên phải có vai trò Lecturer
        if (!await _userManager.IsInRoleAsync(user, AppRoles.Lecturer))
            return null;

        var scopes = await _context.Set<UserRoleScope>()
            .Include(s => s.Role)
            .Where(s => s.UserId == user.Id
                && s.Role != null
                && s.Role.Name == AppRoles.Lecturer)
            .ToListAsync();

        var campaigns = await _context.Campaigns
            .AsNoTracking()
            .Include(c => c.Major)
            .ToListAsync();

        return campaigns
            .Where(c => scopes.Any(s => IsCampaignInScope(s, c)))
            .OrderByDescending(c => c.CreatedAt)
            .ToList();
    }

    // KIỂM TRA PHẠM VI PHÂN QUYỀN CỦA GIẢNG VIÊN
    private static bool IsCampaignInScope(
        UserRoleScope scope,
        Campaign campaign)
    {
        return scope.ScopeType switch
        {
            ScopeTypes.Global => true,

            ScopeTypes.Faculty =>
                scope.FacultyId.HasValue
                && campaign.Major?.FacultyId == scope.FacultyId.Value,

            ScopeTypes.Major =>
                scope.MajorId.HasValue
                && campaign.MajorId == scope.MajorId.Value,

            ScopeTypes.Campaign =>
                scope.CampaignId.HasValue
                && campaign.Id == scope.CampaignId.Value,

            _ => false
        };
    }
    // HIỂN THỊ FORM TẠO ĐỀ TÀI
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null || !user.IsActive)
            return Forbid();

        var campaigns = await GetAccessibleCampaignsAsync(user);

        if (campaigns == null)
            return Forbid();

        var eligibleCampaigns = new List<Campaign>();

        foreach (var campaign in campaigns)
        {
            if (await CanManageTopicAsync(user, campaign))
            {
                eligibleCampaigns.Add(campaign);
            }
        }

        if (!eligibleCampaigns.Any())
        {
            TempData["Error"] = "Hiện tại bạn chưa có đợt đào tạo nào đủ điều kiện đăng ký đề tài.";
            return RedirectToAction(nameof(Index));
        }

        var model = new TopicCreateEditViewModel();

        await PopulateTopicOptionsAsync(model, eligibleCampaigns);

        return View(model);
    }

    // XỬ LÝ TẠO ĐỀ TÀI
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TopicCreateEditViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null || !user.IsActive)
            return Forbid();

        var campaigns = await GetAccessibleCampaignsAsync(user);

        if (campaigns == null)
            return Forbid();

        var campaign = campaigns.FirstOrDefault(c => c.Id == model.CampaignId);

        if (campaign == null)
        {
            ModelState.AddModelError(
                nameof(model.CampaignId),
                "Đợt đào tạo không hợp lệ hoặc bạn không có quyền truy cập.");
        }
        else if (!await CanManageTopicAsync(user, campaign))
        {
            ModelState.AddModelError(
                nameof(model.CampaignId),
                "Bạn chỉ được tạo đề tài khi đợt đào tạo đang mở, còn trong thời hạn và đã được cấp hạn mức.");
        }

        ValidateTopicType(model);
        ValidateTopicFiles(model.Files);

        if (!ModelState.IsValid)
        {
            await PopulateTopicOptionsAsync(model, campaigns);
            return View(model);
        }

        var topic = new Topic
        {
            CampaignId = model.CampaignId,
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            Objectives = model.Objectives,
            Specialty = model.Specialty,
            Technologies = model.Technologies,
            Requirements = model.Requirements,
            TopicType = model.TopicType,
            MaxStudents = model.TopicType == TopicTypes.Individual
                ? model.MaxStudents : null,
            MaxGroups = model.TopicType == TopicTypes.Group
                ? model.MaxGroups : null,
            Status = model.Status,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Topics.Add(topic);
        await _context.SaveChangesAsync();

        await UploadTopicAttachmentsAsync(
            topic,
            model.Files,
            user.Id);

        TempData["Success"] = "Tạo đề tài thành công.";

        return RedirectToAction(nameof(Details), new { id = topic.Id });
    }
        // HIỂN THỊ FORM SỬA ĐỀ TÀI
    [HttpGet]
    public async Task<IActionResult> Edit(ulong id)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null || !user.IsActive)
            return Forbid();

        var topic = await _context.Topics
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (topic == null)
            return NotFound();

        var campaigns = await GetAccessibleCampaignsAsync(user);

        if (campaigns == null
            || !campaigns.Any(c => c.Id == topic.CampaignId))
            return Forbid();

        var isAdmin = User.IsInRole(AppRoles.Admin);

        if (!isAdmin && topic.CreatedByUserId != user.Id)
            return Forbid();

        var campaign = campaigns.First(c => c.Id == topic.CampaignId);

        if (!await CanManageTopicAsync(user, campaign))
        {
            TempData["Error"] = "Đợt đào tạo đã đóng hoặc bạn không còn đủ điều kiện chỉnh sửa đề tài.";
            return RedirectToAction(nameof(Index));
        }
        var model = new TopicCreateEditViewModel
        {
            Id = topic.Id,
            CampaignId = topic.CampaignId,
            Title = topic.Title,
            Description = topic.Description,
            Objectives = topic.Objectives,
            Specialty = topic.Specialty,
            Technologies = topic.Technologies,
            Requirements = topic.Requirements,
            TopicType = topic.TopicType,
            MaxStudents = topic.MaxStudents,
            MaxGroups = topic.MaxGroups,
            Status = topic.Status,

            ExistingAttachments = topic.Attachments.Select(a =>
                new TopicAttachmentViewModel
                {
                    Id = a.Id,
                    OriginalFileName = a.OriginalFileName,
                    FileSize = a.FileSize,
                    ContentType = a.ContentType
                }).ToList()
        };

        await PopulateTopicOptionsAsync(
            model,
            campaigns.Where(c => c.Id == topic.CampaignId).ToList());

        return View(model);
    }

    // XỬ LÝ SỬA ĐỀ TÀI
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        ulong id,
        TopicCreateEditViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null || !user.IsActive)
            return Forbid();

        var topic = await _context.Topics
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (topic == null)
            return NotFound();

        var campaigns = await GetAccessibleCampaignsAsync(user);

        if (campaigns == null
            || !campaigns.Any(c => c.Id == topic.CampaignId))
            return Forbid();

        var isAdmin = User.IsInRole(AppRoles.Admin);

        if (!isAdmin && topic.CreatedByUserId != user.Id)
            return Forbid();

        var campaign = campaigns.First(c => c.Id == topic.CampaignId);

        if (!await CanManageTopicAsync(user, campaign))
        {
            TempData["Error"] = "Đợt đào tạo đã đóng hoặc bạn không còn đủ điều kiện chỉnh sửa đề tài.";
            return RedirectToAction(nameof(Index));
        }
        if (model.CampaignId != topic.CampaignId)
        {
            ModelState.AddModelError(
                nameof(model.CampaignId),
                "Không thể thay đổi đợt đào tạo của đề tài.");
        }

        ValidateTopicType(model);
        ValidateTopicFiles(model.Files);

        if (!ModelState.IsValid)
        {
            model.ExistingAttachments = topic.Attachments.Select(a =>
                new TopicAttachmentViewModel
                {
                    Id = a.Id,
                    OriginalFileName = a.OriginalFileName,
                    FileSize = a.FileSize,
                    ContentType = a.ContentType
                }).ToList();

            await PopulateTopicOptionsAsync(
                model,
                campaigns.Where(c => c.Id == topic.CampaignId).ToList());

            return View(model);
        }

        topic.Title = model.Title.Trim();
        topic.Description = model.Description.Trim();
        topic.Objectives = model.Objectives;
        topic.Specialty = model.Specialty;
        topic.Technologies = model.Technologies;
        topic.Requirements = model.Requirements;
        topic.TopicType = model.TopicType;
        topic.MaxStudents = model.TopicType == TopicTypes.Individual
            ? model.MaxStudents : null;
        topic.MaxGroups = model.TopicType == TopicTypes.Group
            ? model.MaxGroups : null;
        topic.Status = model.Status;
        topic.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await UploadTopicAttachmentsAsync(
            topic,
            model.Files,
            user.Id);

    TempData["Success"] = "Cập nhật đề tài thành công.";

        return RedirectToAction(nameof(Details), new { id = topic.Id });
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAttachment(ulong id)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null || !user.IsActive)
            return Forbid();

        var attachment = await _context.Set<TopicAttachment>()
            .Include(a => a.Topic)
                .ThenInclude(topic => topic!.Campaign)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attachment == null)
            return NotFound();

        var topic = attachment.Topic;

        if (topic == null)
            return NotFound();

        var campaign = topic.Campaign;

        if (campaign == null)
            return NotFound();

        var accessibleCampaigns = await GetAccessibleCampaignsAsync(user);

        if (accessibleCampaigns == null)
            return Forbid();

        if (!accessibleCampaigns.Any(c => c.Id == topic.CampaignId))
            return Forbid();

        var isAdmin = await _userManager.IsInRoleAsync(user, AppRoles.Admin);

        if (!isAdmin && topic.CreatedByUserId != user.Id)
            return Forbid();

        if (!await CanManageTopicAsync(user, campaign))
            return Forbid();

        var uploadFolder = Path.Combine(
            _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"),
            "uploads",
            "topics");

        var filePath = Path.Combine(
            uploadFolder,
            attachment.StoredFileName);

        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }

        _context.Set<TopicAttachment>().Remove(attachment);

        await _context.SaveChangesAsync();

        TempData["Success"] = "Xóa tệp đính kèm thành công.";

        return RedirectToAction(nameof(Edit), new { id = topic.Id });
    }
        // THAY ĐỔI TRẠNG THÁI ĐỀ TÀI
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(
        ulong id,
        string status)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null || !user.IsActive)
            return Forbid();

        var topic = await _context.Topics
            .FirstOrDefaultAsync(t => t.Id == id);

        if (topic == null)
            return NotFound();

        var campaigns = await GetAccessibleCampaignsAsync(user);

        if (campaigns == null
            || !campaigns.Any(c => c.Id == topic.CampaignId))
            return Forbid();

        var isAdmin = User.IsInRole(AppRoles.Admin);

        if (!isAdmin && topic.CreatedByUserId != user.Id)
            return Forbid();

        var campaign = campaigns.First(c => c.Id == topic.CampaignId);

        if (!await CanManageTopicAsync(user, campaign))
        {
            TempData["Error"] = "Đợt đào tạo đã đóng hoặc bạn không còn đủ điều kiện thay đổi trạng thái đề tài.";
            return RedirectToAction(nameof(Index));
        }
        if (status != TopicStatuses.Published
            && status != TopicStatuses.Hidden
            && status != TopicStatuses.Closed)
        {
            return BadRequest("Trạng thái đề tài không hợp lệ.");
        }

        topic.Status = status;
        topic.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Thay đổi trạng thái đề tài thành công.";

        return RedirectToAction(nameof(Details), new { id = topic.Id });
    }
    // KIỂM TRA HÌNH THỨC ĐỀ TÀI
    private void ValidateTopicType(TopicCreateEditViewModel model)
    {
        if (model.TopicType == TopicTypes.Individual)
        {
            if (!model.MaxStudents.HasValue || model.MaxStudents < 1)
            {
                ModelState.AddModelError(
                    nameof(model.MaxStudents),
                    "Vui lòng nhập số sinh viên tối đa.");
            }

            model.MaxGroups = null;
        }
        else if (model.TopicType == TopicTypes.Group)
        {
            if (!model.MaxGroups.HasValue || model.MaxGroups < 1)
            {
                ModelState.AddModelError(
                    nameof(model.MaxGroups),
                    "Vui lòng nhập số nhóm tối đa.");
            }

            model.MaxStudents = null;
        }
        else
        {
            ModelState.AddModelError(
                nameof(model.TopicType),
                "Hình thức đề tài không hợp lệ.");
        }

        if (model.Status != TopicStatuses.Published
            && model.Status != TopicStatuses.Hidden
            && model.Status != TopicStatuses.Closed)
        {
            ModelState.AddModelError(
                nameof(model.Status),
                "Trạng thái đề tài không hợp lệ.");
        }
    }

    // TẢI TỆP ĐÍNH KÈM
    [HttpGet]
    public async Task<IActionResult> DownloadAttachment(ulong id)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null || !user.IsActive)
            return Forbid();

        var attachment = await _context.Set<TopicAttachment>()
            .Include(a => a.Topic)
                .ThenInclude(topic => topic!.Campaign)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attachment == null)
            return NotFound();

        var topic = attachment.Topic;

        if (topic == null || topic.Campaign == null)
            return NotFound();

        var accessibleCampaigns = await GetAccessibleCampaignsAsync(user);

        if (accessibleCampaigns == null ||
            !accessibleCampaigns.Any(c => c.Id == topic.CampaignId))
            return Forbid();

        var uploadFolder = Path.Combine(
            _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"),
            "uploads",
            "topics");

        var filePath = Path.Combine(
            uploadFolder,
            Path.GetFileName(attachment.StoredFileName));

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        return PhysicalFile(
            filePath,
            string.IsNullOrWhiteSpace(attachment.ContentType)
                ? "application/octet-stream"
                : attachment.ContentType,
                attachment.OriginalFileName);
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
            .Include(t => t.CreatedByUser)
            .Include(t => t.SourceTopic)
            .Include(t => t.Attachments)
                .ThenInclude(a => a.UploadedByUser)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (topic == null)
            return NotFound();

        var campaigns = await GetAccessibleCampaignsAsync(user);

        if (campaigns == null
            || !campaigns.Any(c => c.Id == topic.CampaignId))
        {
            return Forbid();
        }

        var isAdmin = User.IsInRole(AppRoles.Admin);
        var canEdit = isAdmin || topic.CreatedByUserId == user.Id;

        var model = new TopicDetailsViewModel
        {
            Id = topic.Id,
            CampaignId = topic.CampaignId,
            CampaignName = topic.Campaign?.Name ?? string.Empty,
            CampaignKind = topic.Campaign?.Kind ?? string.Empty,
            Title = topic.Title,
            Description = topic.Description,
            Objectives = topic.Objectives,
            Specialty = topic.Specialty,
            Technologies = topic.Technologies,
            Requirements = topic.Requirements,
            TopicType = topic.TopicType,
            MaxStudents = topic.MaxStudents,
            MaxGroups = topic.MaxGroups,
            Status = topic.Status,
            CreatedByName = topic.CreatedByUser?.FullName ?? "Không xác định",
            CreatedAt = topic.CreatedAt,
            UpdatedAt = topic.UpdatedAt,
            SourceTopicId = topic.SourceTopicId,
            SourceTopicTitle = topic.SourceTopic?.Title,
            CanEdit = canEdit,
            CanCopy = canEdit,

            Attachments = topic.Attachments.Select(a => new TopicAttachmentViewModel
            {
                Id = a.Id,
                OriginalFileName = a.OriginalFileName,
                FileSize = a.FileSize,
                ContentType = a.ContentType,
                UploadedByName = a.UploadedByUser?.FullName ?? "Không xác định",
                UploadedAt = a.UploadedAt
            }).ToList()
        };

        return View(model);
    }
    private async Task PopulateTopicOptionsAsync(
        TopicCreateEditViewModel model,
        List<Campaign> campaigns)
    {
        model.CampaignOptions = campaigns
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            })
            .ToList();

        model.TopicTypeOptions = new List<SelectListItem>
        {
            new()
            {
                Value = TopicTypes.Individual,
                Text = "Đề tài cá nhân"
            },
            new()
            {
                Value = TopicTypes.Group,
                Text = "Đề tài nhóm"
            }
        };

        model.StatusOptions = new List<SelectListItem>
        {
            new()
            {
                Value = TopicStatuses.Published,
                Text = "Đang công khai"
            },
            new()
            {
                Value = TopicStatuses.Hidden,
                Text = "Đang ẩn"
            },
            new()
            {
                Value = TopicStatuses.Closed,
                Text = "Đã đóng"
            }
        };
    }
        private async Task<bool> CanManageTopicAsync(
        ApplicationUser user,
        Campaign campaign)
    {
        // Admin được quản lý đề tài không phụ thuộc thời gian
        if (await _userManager.IsInRoleAsync(user, AppRoles.Admin))
            return true;

        // Chỉ giảng viên mới được quản lý đề tài theo hạn mức
        if (!await _userManager.IsInRoleAsync(user, AppRoles.Lecturer))
            return false;

        // Đợt đào tạo phải đang mở
        if (campaign.Status != CampaignStatuses.Open)
            return false;

        // Phải nằm trong thời gian đăng ký đề tài
        var today = DateTime.Today;

        if (today < campaign.StartDate.Date ||
            today > campaign.EndDate.Date)
            return false;

        // Giảng viên phải có hạn mức trong đợt đào tạo
        return await _context.LecturerCampaignQuotas.AnyAsync(q =>
            q.CampaignId == campaign.Id &&
            q.LecturerUserId == user.Id);
    }
    private bool ValidateTopicFiles(List<IFormFile> files)
    {
        if (files == null || !files.Any())
        return true;
        var allowedExtensions = new[]
        {
            ".pdf", ".doc", ".docx",
            ".ppt", ".pptx",
            ".xls", ".xlsx",
            ".jpg", ".jpeg", ".png"
        };

        const long maxFileSize = 10 * 1024 * 1024;

        foreach (var file in files)
        {
            if (file == null || file.Length == 0)
                continue;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(
                    "Files",
                    $"File {file.FileName} không đúng định dạng cho phép.");
                return false;
            }

            if (file.Length > maxFileSize)
            {
                ModelState.AddModelError(
                    "Files",
                    $"File {file.FileName} vượt quá dung lượng 10 MB.");
                return false;
            }
        }

        return true;
    }

    private async Task UploadTopicAttachmentsAsync(
        Topic topic,
        List<IFormFile> files,
        string userId)
    {
        if (files == null || !files.Any())
            return;

        var uploadFolder = Path.Combine(
            _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"),
            "uploads",
            "topics");

        Directory.CreateDirectory(uploadFolder);

        foreach (var file in files)
        {
            if (file == null || file.Length == 0)
                continue;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var storedFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadFolder, storedFileName);

            await using (var stream = new FileStream(filePath, FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            var attachment = new TopicAttachment
            {
                TopicId = topic.Id,
                OriginalFileName = Path.GetFileName(file.FileName),
                StoredFileName = storedFileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                UploadedByUserId = userId,
                UploadedAt = DateTime.Now
            };

            _context.Set<TopicAttachment>().Add(attachment);
        }

        await _context.SaveChangesAsync();
    }
}
