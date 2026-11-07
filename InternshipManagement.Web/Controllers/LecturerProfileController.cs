using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels;

namespace InternshipManagement.Web.Controllers;

[Authorize]
public class LecturerProfileController : Controller
{
    private readonly ILecturerProfileService _lecturerService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ProfileAccessService _access;

    public LecturerProfileController(
        ILecturerProfileService lecturerService,
        UserManager<ApplicationUser> userManager, ProfileAccessService access)
    {
        _lecturerService = lecturerService;
        _userManager = userManager;
        _access = access;
    }

    [HttpGet]
    public async Task<IActionResult> Index(ulong? id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();

        var isLecturer = User.IsInRole(AppRoles.Lecturer);
        var isAdminOrManager = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.TrainingManager) || User.IsInRole(AppRoles.FacultyManager);

        LecturerProfileViewModel? model;

        if (id.HasValue && isAdminOrManager)
        {
            var profile = await _lecturerService.GetProfileByIdAsync(id.Value);
            if (profile == null) return NotFound("Không tìm thấy hồ sơ giảng viên.");
            if (!await _access.LecturerAsync(User, profile)) return Forbid();
            model = await _lecturerService.GetProfileViewModelByUserIdAsync(profile.UserId);
        }
        else
        {
            model = await _lecturerService.GetProfileViewModelByUserIdAsync(currentUserId);
            if (model == null)
            {
                if (isLecturer)
                {
                    TempData["ErrorMessage"] = "Tài khoản của bạn chưa được liên kết với hồ sơ giảng viên. Vui lòng liên hệ ban quản trị.";
                    return RedirectToAction("Index", "Home");
                }
                return NotFound("Không tìm thấy hồ sơ giảng viên.");
            }
        }

        if (model == null) return NotFound("Không tìm thấy hồ sơ giảng viên.");
        model.CanEdit = (isLecturer && model.UserId == currentUserId) || User.IsInRole(AppRoles.Admin);

        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Lecturer},{AppRoles.Admin}")]
    public async Task<IActionResult> Edit()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();

        var profile = await _lecturerService.GetProfileViewModelByUserIdAsync(currentUserId);
        if (profile == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ giảng viên để cập nhật.";
            return RedirectToAction(nameof(Index));
        }

        var editModel = new LecturerProfileEditViewModel
        {
            Id = profile.Id,
            FullName = profile.FullName,
            LecturerCode = profile.LecturerCode,
            FacultyName = profile.FacultyName,
            DepartmentName = profile.DepartmentName,
            AcademicDegree = profile.AcademicDegree,
            AcademicTitle = profile.AcademicTitle,
            Expertise = profile.Expertise,
            ResearchInterests = profile.ResearchInterests
        };

        return View(editModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Lecturer},{AppRoles.Admin}")]
    public async Task<IActionResult> Edit(LecturerProfileEditViewModel model)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();

        if (!ModelState.IsValid)
        {
            var currentProfile = await _lecturerService.GetProfileViewModelByUserIdAsync(currentUserId);
            if (currentProfile != null)
            {
                model.FullName = currentProfile.FullName;
                model.LecturerCode = currentProfile.LecturerCode;
                model.FacultyName = currentProfile.FacultyName;
            }
            return View(model);
        }

        var result = await _lecturerService.UpdateProfileAsync(currentUserId, model);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return View(model);
        }

        TempData["SuccessMessage"] = "Cập nhật hồ sơ chuyên môn giảng viên thành công!";
        return RedirectToAction(nameof(Index));
    }
}
