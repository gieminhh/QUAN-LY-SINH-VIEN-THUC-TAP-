using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels;

namespace InternshipManagement.Web.Controllers;

[Authorize]
public class StudentProfileController : Controller
{
    private readonly IStudentProfileService _profileService;
    private readonly IFileStorageService _fileStorage;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ProfileAccessService _access;

    public StudentProfileController(
        IStudentProfileService profileService,
        IFileStorageService fileStorage,
        UserManager<ApplicationUser> userManager, ProfileAccessService access)
    {
        _profileService = profileService;
        _fileStorage = fileStorage;
        _userManager = userManager;
        _access = access;
    }

    [HttpGet]
    public async Task<IActionResult> Index(ulong? id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();

        var isStudent = User.IsInRole(AppRoles.Student);
        var isAdminOrManager = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.TrainingManager) || User.IsInRole(AppRoles.FacultyManager);

        StudentProfileViewModel? model;

        if (id.HasValue && (isAdminOrManager || User.IsInRole(AppRoles.Lecturer) || User.IsInRole(AppRoles.Company)))
        {
            var profile = await _profileService.GetProfileByIdAsync(id.Value);
            if (profile == null) return NotFound("Không tìm thấy hồ sơ sinh viên.");
            if (!await _access.StudentAsync(User, profile)) return Forbid();
            model = await _profileService.GetProfileViewModelByUserIdAsync(profile.UserId);
        }
        else
        {
            // If student, always enforce viewing own profile (US-07 AC-6)
            model = await _profileService.GetProfileViewModelByUserIdAsync(currentUserId);
            if (model == null)
            {
                if (isStudent)
                {
                    TempData["ErrorMessage"] = "Tài khoản của bạn chưa được liên kết với hồ sơ sinh viên. Vui lòng liên hệ ban quản trị.";
                    return RedirectToAction("Index", "Home");
                }
                return NotFound("Không tìm thấy hồ sơ sinh viên.");
            }
        }

        if (model == null) return NotFound("Không tìm thấy hồ sơ sinh viên.");
        model.CanEdit = isStudent && model.UserId == currentUserId;

        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<IActionResult> Edit()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();

        var profile = await _profileService.GetProfileViewModelByUserIdAsync(currentUserId);
        if (profile == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ sinh viên để chỉnh sửa.";
            return RedirectToAction(nameof(Index));
        }

        var editModel = new StudentProfileEditViewModel
        {
            Id = profile.Id,
            FullName = profile.FullName,
            StudentCode = profile.StudentCode,
            ClassName = profile.ClassName,
            MajorName = profile.MajorName,
            LatestGpa4 = profile.LatestGpa4,
            LatestGpa10 = profile.LatestGpa10,
            Skills = profile.Skills,
            Interests = profile.Interests,
            CurrentCvAttachment = profile.CvAttachment
        };

        return View(editModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<IActionResult> Edit(StudentProfileEditViewModel model)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();

        // Security check: Only update allowed fields (Skills, Interests, CvFile) for the current user
        // StudentCode and GPA are strictly excluded from update payload
        if (!ModelState.IsValid) { TempData["ErrorMessage"]="Thông tin hồ sơ không hợp lệ.";return RedirectToAction(nameof(Edit)); }
        var result = await _profileService.UpdateProfileAsync(currentUserId, model.Skills, model.Interests, model.CvFile);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            var currentProfile = await _profileService.GetProfileViewModelByUserIdAsync(currentUserId);
            if (currentProfile != null)
            {
                model.FullName = currentProfile.FullName;
                model.StudentCode = currentProfile.StudentCode;
                model.ClassName = currentProfile.ClassName;
                model.MajorName = currentProfile.MajorName;
                model.LatestGpa4 = currentProfile.LatestGpa4;
                model.LatestGpa10 = currentProfile.LatestGpa10;
                model.CurrentCvAttachment = currentProfile.CvAttachment;
            }
            return View(model);
        }

        TempData["SuccessMessage"] = "Cập nhật thông tin hồ sơ và CV thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadCv(ulong attachmentId)
    {
        var attachment = await _profileService.GetCvAttachmentAsync(attachmentId);
        if (attachment == null) return NotFound("Tệp tin CV không tồn tại.");

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isOwner = attachment.StudentProfile?.UserId == currentUserId;
        if (attachment.StudentProfile == null || !await _access.StudentAsync(User, attachment.StudentProfile)) return Forbid();

        if (!_fileStorage.FileExists(attachment.StorageKey))
        {
            return NotFound("Tệp tin không tìm thấy trên hệ thống lưu trữ.");
        }

        var fullPath = _fileStorage.GetFullPath(attachment.StorageKey);
        return PhysicalFile(fullPath, attachment.ContentType, attachment.OriginalFileName);
    }
}
