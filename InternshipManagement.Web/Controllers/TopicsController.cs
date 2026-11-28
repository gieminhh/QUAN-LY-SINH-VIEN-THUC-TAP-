
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.ViewModels.Topics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;

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

[HttpGet]
public async Task<IActionResult> Index(TopicListViewModel model)
{
    var user = await _userManager.GetUserAsync(User);

    if (user == null || !user.IsActive)
        return Forbid();

    var isAdmin = User.IsInRole(AppRoles.Admin);

    // Lấy các đợt đào tạo mà người dùng được phép truy cập
    var campaigns = await GetAccessibleCampaignsAsync(user);

    if (campaigns == null)
        return Forbid();

    var campaignIds = campaigns.Select(c => c.Id).ToList();

    // Truy vấn đề tài trong phạm vi được phân quyền
    var query = _context.Topics
        .AsNoTracking()
        .Include(t => t.Campaign)
            .ThenInclude(c => c!.Term)
                .ThenInclude(term => term!.AcademicYear)
        .Include(t => t.Major)
            .ThenInclude(m => m!.Faculty)
        .Include(t => t.CreatedByUser)
        .Include(t => t.Lecturers)
            .ThenInclude(l => l.Lecturer)
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

    // Lọc theo loại đề tài
    if (model.TypeFilter != "ALL")
    {
        query = query.Where(t =>
            t.TopicType == model.TypeFilter);
    }

    // Lọc theo trạng thái đề tài
    if (model.StatusFilter != "ALL")
    {
        query = query.Where(t =>
            t.Status == model.StatusFilter);
    }

    // Lọc theo năm học
    if (model.AcademicYearFilter.HasValue)
    {
        var yearId = model.AcademicYearFilter.Value;

        query = query.Where(t =>
            t.Campaign != null &&
            t.Campaign.Term != null &&
            t.Campaign.Term.AcademicYearId == yearId);
    }

    // Lọc theo khoa
    if (model.FacultyFilter.HasValue)
    {
        var facultyId = model.FacultyFilter.Value;

        query = query.Where(t =>
            t.Major != null &&
            t.Major.FacultyId == facultyId);
    }

    // Lọc theo giảng viên
    if (!string.IsNullOrWhiteSpace(model.LecturerFilter))
    {
        var lecturerId = model.LecturerFilter;

        query = query.Where(t =>
            t.Lecturers.Any(l =>
                l.LecturerUserId == lecturerId));
    }

    // Tính tổng số đề tài
    model.TotalCount = await query.CountAsync();

    model.PageSize = 10;

    if (model.Page < 1)
        model.Page = 1;

    if (model.TotalPages > 0 && model.Page > model.TotalPages)
        model.Page = model.TotalPages;

    // Lấy danh sách đề tài theo phân trang
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

            // Trạng thái duyệt đề tài
            ApprovalStatus = t.ApprovalStatus,

            MaxStudents = t.MaxStudents,
            MaxGroups = t.MaxGroups,
            CreatedByName = t.CreatedByUser!.FullName,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            CanEdit = isAdmin || t.CreatedByUserId == user.Id
        })
        .ToListAsync();

    // Danh sách đợt đào tạo được phép truy cập
    model.CampaignOptions = campaigns
        .Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = c.Name,
            Selected = model.CampaignFilter == c.Id
        })
        .ToList();

    // Danh sách năm học thuộc các đợt được phép truy cập
    model.AcademicYearOptions = await _context.AcademicYears
        .AsNoTracking()
        .Where(y => _context.Campaigns.Any(c =>
            campaignIds.Contains(c.Id) &&
            c.Term != null &&
            c.Term.AcademicYearId == y.Id))
        .OrderByDescending(y => y.Code)
        .Select(y => new SelectListItem
        {
            Value = y.Id.ToString(),
            Text = y.Code,
            Selected = model.AcademicYearFilter == y.Id
        })
        .ToListAsync();

    // Danh sách khoa thuộc các đợt được phép truy cập
    model.FacultyOptions = await _context.Faculties
        .AsNoTracking()
        .Where(f => f.IsActive &&
            _context.Campaigns.Any(c =>
                campaignIds.Contains(c.Id) &&
                c.Major != null &&
                c.Major.FacultyId == f.Id))
        .OrderBy(f => f.Name)
        .Select(f => new SelectListItem
        {
            Value = f.Id.ToString(),
            Text = f.Name,
            Selected = model.FacultyFilter == f.Id
        })
        .ToListAsync();

    // Danh sách giảng viên được gán đề tài trong phạm vi truy cập
    model.LecturerOptions = await _context.TopicLecturers
        .AsNoTracking()
        .Where(l =>
            l.Topic != null &&
            campaignIds.Contains(l.Topic.CampaignId))
        .Select(l => new
        {
            l.LecturerUserId,
            LecturerName = l.Lecturer!.FullName
        })
        .Distinct()
        .OrderBy(l => l.LecturerName)
        .Select(l => new SelectListItem
        {
            Value = l.LecturerUserId,
            Text = l.LecturerName,
            Selected = model.LecturerFilter == l.LecturerUserId
        })
        .ToListAsync();

    return View(model);
}

    // KHO ĐỀ TÀI CÁC KHÓA TRƯỚC
[HttpGet]
public async Task<IActionResult> Archive()
{
    var user = await _userManager.GetUserAsync(User);

    if (user == null || !user.IsActive)
        return Forbid();

    // Lấy các đợt đào tạo mà người dùng có quyền truy cập
    var campaigns = await GetAccessibleCampaignsAsync(user);

    if (campaigns == null)
        return Forbid();

    var campaignIds = campaigns
        .Where(c => c.Status == CampaignStatuses.Closed
                 || c.Status == CampaignStatuses.Archived)
        .Select(c => c.Id)
        .ToList();

    // Lấy đề tài thuộc các đợt đào tạo đã kết thúc
    var topics = await _context.Topics
        .AsNoTracking()
        .Include(t => t.Campaign)
        .Include(t => t.CreatedByUser)
        .Where(t => campaignIds.Contains(t.CampaignId))
        .OrderByDescending(t => t.CreatedAt)
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
            UpdatedAt = t.UpdatedAt
        })
        .ToListAsync();

    return View(topics);
}

private async Task<List<SimilarTopicWarningViewModel>> FindSimilarTopicsAsync(
    TopicCreateEditViewModel model,
    List<ulong> campaignIds,
    ulong? excludeTopicId = null)
{
    var existingTopics = await _context.Topics
        .AsNoTracking()
        .Where(t => campaignIds.Contains(t.CampaignId)
            && (!excludeTopicId.HasValue || t.Id != excludeTopicId.Value))
        .Select(t => new
        {
            t.Id,
            t.Title,
            t.Description,
            t.Objectives,
            t.Specialty,
            t.Technologies,
            t.Requirements,
            CampaignName = t.Campaign!.Name,
            t.Status
        })
        .ToListAsync();

    var newTitle = GetWords(model.Title);
    var newContent = GetWords(string.Join(" ",
        model.Description,
        model.Objectives,
        model.Specialty,
        model.Technologies,
        model.Requirements));

    var results = new List<SimilarTopicWarningViewModel>();

    foreach (var topic in existingTopics)
    {
        var oldTitle = GetWords(topic.Title);

        var oldContent = GetWords(string.Join(" ",
            topic.Description,
            topic.Objectives,
            topic.Specialty,
            topic.Technologies,
            topic.Requirements));

        double titleScore = CalculateSimilarity(newTitle, oldTitle);
        double contentScore = CalculateSimilarity(newContent, oldContent);

        double totalScore = titleScore * 0.65 + contentScore * 0.35;

        // Chỉ đưa vào danh sách khi có dấu hiệu tương đồng
        if (titleScore >= 0.25 || totalScore >= 0.28)
        {
            results.Add(new SimilarTopicWarningViewModel
            {
                Id = topic.Id,
                Title = topic.Title,
                CampaignName = topic.CampaignName,
                Status = topic.Status,
                SimilarityPercent = Math.Round(totalScore * 100, 1)
            });
        }
    }

    return results
        .OrderByDescending(t => t.SimilarityPercent)
        .Take(5)
        .ToList();
}

private static HashSet<string> GetWords(string? text)
{
    if (string.IsNullOrWhiteSpace(text))
        return new HashSet<string>();

    return Regex.Matches(
            text.ToLowerInvariant(),
            @"[\p{L}\p{N}]{2,}")
        .Select(m => m.Value)
        .ToHashSet();
}

private static double CalculateSimilarity(
    HashSet<string> first,
    HashSet<string> second)
{
    if (first.Count == 0 || second.Count == 0)
        return 0;

    int intersection = first.Intersect(second).Count();
    int union = first.Union(second).Count();

    return union == 0 ? 0 : (double)intersection / union;
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
        await PopulateTopicOptionsAsync(
            model,
            campaigns.Where(c => c.Id == model.CampaignId).ToList());

        return View(model);
    }

    // KIỂM TRA ĐỀ TÀI CÓ TƯƠNG ĐỒNG HAY KHÔNG
    if (!model.ConfirmSimilarTopics)
    {
        var campaignIds = campaigns.Select(c => c.Id).ToList();

        model.SimilarTopics = await FindSimilarTopicsAsync(
            model,
            campaignIds);

        // Nếu phát hiện đề tài tương đồng thì hiển thị cảnh báo
        if (model.SimilarTopics.Any())
        {
            await PopulateTopicOptionsAsync(
                model,
                campaigns.Where(c => c.Id == model.CampaignId).ToList());

            return View(model);
        }
    }

    // TIẾN HÀNH TẠO ĐỀ TÀI
    var topic = new Topic
    {
        CampaignId = model.CampaignId,
        MajorId = campaign.MajorId,
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
            ApprovalStatus = topic.ApprovalStatus,
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

    // ADMIN DUYỆT ĐỀ TÀI
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(ulong id)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null || !user.IsActive)
            return Forbid();

        // Chỉ Admin được duyệt
        if (!User.IsInRole(AppRoles.Admin))
            return Forbid();

        var topic = await _context.Topics
            .FirstOrDefaultAsync(t => t.Id == id);

        if (topic == null)
            return NotFound();

        if (topic.ApprovalStatus != TopicApprovalStatuses.Pending)
        {
            TempData["Error"] = "Đề tài không ở trạng thái chờ duyệt.";
            return RedirectToAction(nameof(Details), new { id });
        }

        topic.ApprovalStatus = TopicApprovalStatuses.Approved;
        topic.ReviewedByUserId = user.Id;
        topic.ReviewedAt = DateTime.UtcNow;
        topic.RejectionReason = null;
        topic.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã duyệt đề tài thành công.";

        return RedirectToAction(nameof(Details), new { id });
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
