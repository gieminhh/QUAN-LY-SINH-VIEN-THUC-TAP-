using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels.Profiles;

namespace InternshipManagement.Web.Controllers;

[Authorize]
public class ProfilesController : Controller
{
    private const long MaxCvSize = 5 * 1024 * 1024;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserScopeService _scopeService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ProfilesController> _logger;

    public ProfilesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IUserScopeService scopeService,
        IWebHostEnvironment environment,
        ILogger<ProfilesController> logger)
    {
        _context = context;
        _userManager = userManager;
        _scopeService = scopeService;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var model = await CreateProfileViewModelAsync(user);
        if (!model.IsStudent && !model.IsLecturer)
            return Forbid();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileEditViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var roles = await _userManager.GetRolesAsync(user);
        model.IsStudent = roles.Contains(AppRoles.Student);
        model.IsLecturer = roles.Contains(AppRoles.Lecturer);
        model.ExistingCvFileName = user.CvStoredFileName;
        await PopulateMajorOptionsAsync(model);

        if (!model.IsStudent && !model.IsLecturer)
            return Forbid();

        if (model.IsStudent && model.MajorId.HasValue &&
            !await _context.Majors.AnyAsync(m => m.Id == model.MajorId && m.IsActive))
        {
            ModelState.AddModelError(nameof(model.MajorId), "Ngành đã chọn không tồn tại hoặc không còn hoạt động.");
        }

        string? newCvFileName = null;
        if (model.CvFile != null)
        {
            if (model.CvFile.Length == 0 || model.CvFile.Length > MaxCvSize ||
                !string.Equals(Path.GetExtension(model.CvFile.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.CvFile), "CV phải là tệp PDF có dung lượng tối đa 5 MB.");
            }
            else
            {
                await using var stream = model.CvFile.OpenReadStream();
                var header = new byte[5];
                var bytesRead = await stream.ReadAsync(header);
                if (bytesRead != header.Length || !header.AsSpan().SequenceEqual("%PDF-"u8))
                    ModelState.AddModelError(nameof(model.CvFile), "Tệp tải lên không có định dạng PDF hợp lệ.");
            }
        }

        if (!ModelState.IsValid)
            return View(model);

        if (model.IsStudent)
        {
            user.ClassName = model.ClassName?.Trim();
            user.MajorId = model.MajorId;
            user.Skills = model.Skills?.Trim();
            user.Interests = model.Interests?.Trim();
        }

        if (model.IsLecturer)
            user.LecturerSpecialty = model.LecturerSpecialty?.Trim();

        var oldCvFileName = user.CvStoredFileName;
        if (model.CvFile != null)
        {
            newCvFileName = $"{Guid.NewGuid():N}.pdf";
            try
            {
                var directory = GetCvDirectory();
                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, newCvFileName);
                await using var destination = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await model.CvFile.CopyToAsync(destination);
                user.CvStoredFileName = newCvFileName;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Không thể lưu CV cho người dùng {UserId}.", user.Id);
                ModelState.AddModelError(nameof(model.CvFile), "Không thể lưu CV lúc này. Vui lòng thử lại.");
                return View(model);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Không có quyền lưu CV cho người dùng {UserId}.", user.Id);
                ModelState.AddModelError(nameof(model.CvFile), "Không thể lưu CV do lỗi quyền truy cập.");
                return View(model);
            }
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            if (newCvFileName != null)
                DeleteCvFile(newCvFileName);
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        if (oldCvFileName != null && newCvFileName != null)
            DeleteCvFile(oldCvFileName);

        TempData["SuccessMessage"] = "Đã cập nhật hồ sơ thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveCv()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var oldCvFileName = user.CvStoredFileName;
        user.CvStoredFileName = null;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            _logger.LogError("Không thể cập nhật hồ sơ khi xóa CV của người dùng {UserId}: {Errors}",
                user.Id, string.Join("; ", result.Errors.Select(e => e.Description)));
            TempData["ErrorMessage"] = "Không thể xóa CV lúc này. Vui lòng thử lại.";
            return RedirectToAction(nameof(Index));
        }

        if (oldCvFileName != null)
            DeleteCvFile(oldCvFileName);

        TempData["SuccessMessage"] = "Đã xóa CV khỏi hồ sơ.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadCv(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null || string.IsNullOrWhiteSpace(user.CvStoredFileName))
            return NotFound();

        var currentUserId = _userManager.GetUserId(User);
        var canDownload = user.Id == currentUserId || User.IsInRole(AppRoles.Admin);
        if (!canDownload && user.MajorId.HasValue && currentUserId != null &&
            (User.IsInRole(AppRoles.TrainingManager) || User.IsInRole(AppRoles.FacultyManager)))
        {
            var accessibleCampaigns = await _scopeService.FilterAccessibleCampaignsAsync(
                currentUserId, _context.Campaigns);
            canDownload = await accessibleCampaigns.AnyAsync(c => c.MajorId == user.MajorId);
        }

        if (!canDownload)
            return Forbid();

        var path = Path.Combine(GetCvDirectory(), user.CvStoredFileName);
        if (!System.IO.File.Exists(path))
            return NotFound();

        return PhysicalFile(path, "application/pdf", "CV.pdf");
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.TrainingManager},{AppRoles.FacultyManager}")]
    [HttpGet]
    public async Task<IActionResult> Manage(ulong? campaignId)
    {
        var currentUserId = _userManager.GetUserId(User)!;
        var query = _context.Campaigns
            .Include(c => c.Major)
            .ThenInclude(m => m!.Faculty)
            .AsQueryable();
        var accessibleCampaigns = await _scopeService.FilterAccessibleCampaignsAsync(currentUserId, query);
        var campaigns = await accessibleCampaigns.OrderByDescending(c => c.CreatedAt).ToListAsync();
        if (campaigns.Count == 0)
            return View(new LecturerManagementViewModel());

        var selectedCampaignId = campaignId ?? campaigns[0].Id;
        var selectedCampaign = campaigns.FirstOrDefault(c => c.Id == selectedCampaignId);
        if (selectedCampaign == null)
            return Forbid();

        var lecturers = (await _userManager.GetUsersInRoleAsync(AppRoles.Lecturer))
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .ToList();
        var students = (await _userManager.GetUsersInRoleAsync(AppRoles.Student))
            .Where(u => u.IsActive && u.MajorId == selectedCampaign.MajorId)
            .OrderBy(u => u.FullName)
            .ToList();
        var quotaRows = await _context.LecturerCampaignQuotas
            .Where(q => q.CampaignId == selectedCampaignId)
            .ToDictionaryAsync(q => q.LecturerUserId);
        var assignedCounts = await _context.LecturerCampaignAssignments
            .Where(a => a.CampaignId == selectedCampaignId &&
                (a.Status == LecturerAssignmentStatuses.Assigned ||
                 a.Status == LecturerAssignmentStatuses.ApprovedException))
            .GroupBy(a => a.LecturerUserId)
            .Select(g => new { LecturerUserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.LecturerUserId, g => g.Count);

        var assignments = await _context.LecturerCampaignAssignments
            .Where(a => a.CampaignId == selectedCampaignId)
            .Include(a => a.Lecturer)
            .Include(a => a.Student)
            .Include(a => a.ReviewedByUser)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return View(new LecturerManagementViewModel
        {
            CampaignId = selectedCampaignId,
            CampaignOptions = campaigns.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.Code} - {c.Name}"
            }).ToList(),
            Quotas = lecturers.Select(lecturer =>
            {
                quotaRows.TryGetValue(lecturer.Id, out var quota);
                assignedCounts.TryGetValue(lecturer.Id, out var count);
                return new LecturerQuotaRowViewModel
                {
                    LecturerUserId = lecturer.Id,
                    LecturerName = lecturer.FullName,
                    Specialty = lecturer.LecturerSpecialty ?? string.Empty,
                    QuotaId = quota?.Id,
                    Capacity = quota?.Capacity ?? 0,
                    AssignedCount = count
                };
            }).ToList(),
            LecturerOptions = lecturers
                .Where(l => quotaRows.ContainsKey(l.Id))
                .Select(l => new SelectListItem { Value = l.Id, Text = l.FullName })
                .ToList(),
            StudentOptions = students.Select(s => new SelectListItem
            {
                Value = s.Id,
                Text = $"{s.FullName} ({s.UserName})"
            }).ToList(),
            StudentProfiles = students.Select(s => new StudentProfileRowViewModel
            {
                UserId = s.Id,
                FullName = s.FullName,
                ClassName = s.ClassName ?? string.Empty,
                MajorName = selectedCampaign.Major?.Name ?? string.Empty,
                Skills = s.Skills ?? string.Empty,
                Interests = s.Interests ?? string.Empty,
                HasCv = !string.IsNullOrWhiteSpace(s.CvStoredFileName)
            }).ToList(),
            Assignments = assignments.Select(a => new LecturerAssignmentRowViewModel
            {
                Id = a.Id,
                LecturerName = a.Lecturer?.FullName ?? a.LecturerUserId,
                StudentName = a.Student?.FullName ?? a.StudentUserId,
                Status = a.Status,
                ExceptionReason = a.ExceptionReason,
                ReviewedByName = a.ReviewedByUser?.FullName,
                ReviewedAt = a.ReviewedAt,
                CanReview = a.Status == LecturerAssignmentStatuses.PendingException
            }).ToList(),
            CanReviewExceptions = true
        });
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.TrainingManager},{AppRoles.FacultyManager}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetQuota(SetLecturerQuotaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Dữ liệu quota không hợp lệ.";
            return RedirectToAction(nameof(Manage), new { campaignId = model.CampaignId });
        }

        if (!await CanManageCampaignAsync(model.CampaignId))
            return Forbid();
        if (!await UserIsInRoleAsync(model.LecturerUserId, AppRoles.Lecturer))
        {
            TempData["ErrorMessage"] = "Tài khoản được chọn không có vai trò giảng viên.";
            return RedirectToAction(nameof(Manage), new { campaignId = model.CampaignId });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var quota = await _context.LecturerCampaignQuotas
            .FromSqlInterpolated($"SELECT * FROM `lecturer_campaign_quotas` WHERE `campaign_id` = {model.CampaignId} AND `lecturer_user_id` = {model.LecturerUserId} FOR UPDATE")
            .SingleOrDefaultAsync();
        var assignedCount = await _context.LecturerCampaignAssignments.CountAsync(a =>
            a.CampaignId == model.CampaignId &&
            a.LecturerUserId == model.LecturerUserId &&
            (a.Status == LecturerAssignmentStatuses.Assigned ||
             a.Status == LecturerAssignmentStatuses.ApprovedException));

        if (model.Capacity < assignedCount)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = $"Không thể đặt quota thấp hơn số sinh viên đang được hướng dẫn ({assignedCount}).";
            return RedirectToAction(nameof(Manage), new { campaignId = model.CampaignId });
        }

        if (quota == null)
        {
            quota = new LecturerCampaignQuota
            {
                CampaignId = model.CampaignId,
                LecturerUserId = model.LecturerUserId
            };
            _context.LecturerCampaignQuotas.Add(quota);
        }
        quota.Capacity = model.Capacity;
        quota.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = "Đã lưu quota giảng viên cho đợt.";
        return RedirectToAction(nameof(Manage), new { campaignId = model.CampaignId });
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.TrainingManager},{AppRoles.FacultyManager}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAssignment(AddLecturerAssignmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Thông tin phân công không hợp lệ.";
            return RedirectToAction(nameof(Manage), new { campaignId = model.CampaignId });
        }

        if (!await CanManageCampaignAsync(model.CampaignId))
            return Forbid();
        if (!await UserIsInRoleAsync(model.LecturerUserId, AppRoles.Lecturer) ||
            !await UserIsInRoleAsync(model.StudentUserId, AppRoles.Student))
        {
            TempData["ErrorMessage"] = "Chỉ có thể phân công tài khoản sinh viên cho giảng viên.";
            return RedirectToAction(nameof(Manage), new { campaignId = model.CampaignId });
        }
        var campaignMajorId = await _context.Campaigns
            .Where(c => c.Id == model.CampaignId)
            .Select(c => (ulong?)c.MajorId)
            .SingleOrDefaultAsync();
        var studentMajorId = await _context.Users
            .Where(u => u.Id == model.StudentUserId)
            .Select(u => u.MajorId)
            .SingleOrDefaultAsync();
        if (campaignMajorId == null || studentMajorId != campaignMajorId)
        {
            TempData["ErrorMessage"] = "Chỉ có thể phân công sinh viên thuộc ngành của đợt; hãy cập nhật ngành trong hồ sơ sinh viên.";
            return RedirectToAction(nameof(Manage), new { campaignId = model.CampaignId });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var quota = await _context.LecturerCampaignQuotas
            .FromSqlInterpolated($"SELECT * FROM `lecturer_campaign_quotas` WHERE `campaign_id` = {model.CampaignId} AND `lecturer_user_id` = {model.LecturerUserId} FOR UPDATE")
            .SingleOrDefaultAsync();
        if (quota == null)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Giảng viên chưa được thiết lập quota cho đợt này.";
            return RedirectToAction(nameof(Manage), new { campaignId = model.CampaignId });
        }

        var duplicate = await _context.LecturerCampaignAssignments.AnyAsync(a =>
            a.CampaignId == model.CampaignId &&
            a.StudentUserId == model.StudentUserId &&
            a.Status != LecturerAssignmentStatuses.RejectedException);
        if (duplicate)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Sinh viên đã được phân công hoặc đang có yêu cầu ngoại lệ trong đợt này.";
            return RedirectToAction(nameof(Manage), new { campaignId = model.CampaignId });
        }

        var assignedCount = await _context.LecturerCampaignAssignments.CountAsync(a =>
            a.CampaignId == model.CampaignId &&
            a.LecturerUserId == model.LecturerUserId &&
            (a.Status == LecturerAssignmentStatuses.Assigned ||
             a.Status == LecturerAssignmentStatuses.ApprovedException));
        var isOverQuota = assignedCount >= quota.Capacity;
        if (isOverQuota && string.IsNullOrWhiteSpace(model.ExceptionReason))
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = $"Giảng viên đã đạt quota ({quota.Capacity}). Cần nhập lý do để gửi ngoại lệ chờ duyệt.";
            return RedirectToAction(nameof(Manage), new { campaignId = model.CampaignId });
        }

        _context.LecturerCampaignAssignments.Add(new LecturerCampaignAssignment
        {
            CampaignId = model.CampaignId,
            LecturerUserId = model.LecturerUserId,
            StudentUserId = model.StudentUserId,
            Status = isOverQuota
                ? LecturerAssignmentStatuses.PendingException
                : LecturerAssignmentStatuses.Assigned,
            ExceptionReason = isOverQuota ? model.ExceptionReason!.Trim() : null,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = isOverQuota
            ? "Đã gửi yêu cầu vượt quota để chờ duyệt; sinh viên chưa được tính là đã phân công."
            : "Đã phân công sinh viên cho giảng viên.";
        return RedirectToAction(nameof(Manage), new { campaignId = model.CampaignId });
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.TrainingManager},{AppRoles.FacultyManager}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewException(ulong id, bool approve)
    {
        var assignment = await _context.LecturerCampaignAssignments.FindAsync(id);
        if (assignment == null)
            return NotFound();
        if (!await CanManageCampaignAsync(assignment.CampaignId))
            return Forbid();
        if (assignment.Status != LecturerAssignmentStatuses.PendingException)
        {
            TempData["ErrorMessage"] = "Yêu cầu ngoại lệ này đã được xử lý.";
            return RedirectToAction(nameof(Manage), new { campaignId = assignment.CampaignId });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var quota = await _context.LecturerCampaignQuotas
            .FromSqlInterpolated($"SELECT * FROM `lecturer_campaign_quotas` WHERE `campaign_id` = {assignment.CampaignId} AND `lecturer_user_id` = {assignment.LecturerUserId} FOR UPDATE")
            .SingleOrDefaultAsync();
        if (quota == null)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Không tìm thấy quota của giảng viên; không thể duyệt ngoại lệ.";
            return RedirectToAction(nameof(Manage), new { campaignId = assignment.CampaignId });
        }

        await _context.Entry(assignment).ReloadAsync();
        if (assignment.Status != LecturerAssignmentStatuses.PendingException)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Yêu cầu ngoại lệ đã được xử lý bởi người quản lý khác.";
            return RedirectToAction(nameof(Manage), new { campaignId = assignment.CampaignId });
        }

        assignment.Status = approve
            ? LecturerAssignmentStatuses.ApprovedException
            : LecturerAssignmentStatuses.RejectedException;
        assignment.ReviewedByUserId = _userManager.GetUserId(User);
        assignment.ReviewedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = approve
            ? "Đã duyệt ngoại lệ vượt quota; quyết định được lưu trong hồ sơ phân công."
            : "Đã từ chối yêu cầu ngoại lệ.";
        return RedirectToAction(nameof(Manage), new { campaignId = assignment.CampaignId });
    }

    private async Task<ProfileEditViewModel> CreateProfileViewModelAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var model = new ProfileEditViewModel
        {
            ClassName = user.ClassName,
            MajorId = user.MajorId,
            Skills = user.Skills,
            Interests = user.Interests,
            ExistingCvFileName = user.CvStoredFileName,
            LecturerSpecialty = user.LecturerSpecialty,
            IsStudent = roles.Contains(AppRoles.Student),
            IsLecturer = roles.Contains(AppRoles.Lecturer)
        };
        await PopulateMajorOptionsAsync(model);
        return model;
    }

    private async Task PopulateMajorOptionsAsync(ProfileEditViewModel model)
    {
        model.MajorOptions = await _context.Majors
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .Select(m => new SelectListItem
            {
                Value = m.Id.ToString(),
                Text = $"{m.Name} ({m.Code})",
                Selected = model.MajorId == m.Id
            })
            .ToListAsync();
    }

    private async Task<bool> CanManageCampaignAsync(ulong campaignId)
    {
        var userId = _userManager.GetUserId(User);
        return userId != null && await _scopeService.CanManageCampaignAsync(userId, campaignId);
    }

    private async Task<bool> UserIsInRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user != null && user.IsActive && await _userManager.IsInRoleAsync(user, role);
    }

    private string GetCvDirectory() => Path.Combine(_environment.ContentRootPath, "App_Data", "CVs");

    private void DeleteCvFile(string fileName)
    {
        var path = Path.Combine(GetCvDirectory(), fileName);
        try
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Không thể xóa CV cũ {CvFileName}.", fileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Không có quyền xóa CV cũ {CvFileName}.", fileName);
        }
    }
}
