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
public class ProfilesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
    IUserScopeService scopeService, IStudentProfileService studentProfiles, ILecturerProfileService lecturerProfiles,
    IFileStorageService files, ProfileAccessService access, StudentAssignmentService assignments) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IUserScopeService _scopeService = scopeService;
    string Actor => _userManager.GetUserId(User)!;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!User.IsInRole(AppRoles.Student) && !User.IsInRole(AppRoles.Lecturer)) return Forbid();
        return View(await ProfileModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileEditViewModel model)
    {
        var current = await ProfileModel();
        if (!current.IsStudent && !current.IsLecturer) return Forbid();
        // Academic fields are supplied by staff, never accepted from a student's form.
        ModelState.Remove(nameof(model.ClassName));
        ModelState.Remove(nameof(model.MajorId));
        if (current.IsStudent && model.CvFile != null)
        {
            if (model.CvFile.Length is <= 0 or > 5 * 1024 * 1024 || !string.Equals(Path.GetExtension(model.CvFile.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError(nameof(model.CvFile), "CV phải là PDF có dung lượng tối đa 5 MB.");
            else
            {
                await using var stream = model.CvFile.OpenReadStream();
                var header = new byte[5];
                var length = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
                if (length != 5 || !header.AsSpan().SequenceEqual("%PDF-"u8)) ModelState.AddModelError(nameof(model.CvFile), "Tệp tải lên không có định dạng PDF hợp lệ.");
            }
        }
        if (ModelState.IsValid && current.IsStudent)
        {
            var result = await studentProfiles.UpdateProfileAsync(Actor, model.Skills?.Trim(), model.Interests?.Trim(), model.CvFile);
            if (!result.Success) ModelState.AddModelError("", result.ErrorMessage!);
        }
        if (ModelState.IsValid && current.IsLecturer)
        {
            var profile = await _context.LecturerProfiles.FirstOrDefaultAsync(l => l.UserId == Actor);
            if (profile == null) ModelState.AddModelError("", "Tài khoản chưa được liên kết hồ sơ giảng viên.");
            else { profile.Expertise = model.LecturerSpecialty?.Trim(); profile.UpdatedAt = DateTime.UtcNow; await _context.SaveChangesAsync(); }
        }
        if (!ModelState.IsValid)
        {
            current.Skills = model.Skills; current.Interests = model.Interests; current.LecturerSpecialty = model.LecturerSpecialty;
            return View(current);
        }
        TempData["SuccessMessage"] = "Đã cập nhật hồ sơ thành công.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Student)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveCv()
    {
        var attachments = await _context.FileAttachments.Where(a => a.StudentProfile != null && a.StudentProfile.UserId == Actor && a.FileCategory == FileCategories.StudentCv).ToListAsync();
        _context.FileAttachments.RemoveRange(attachments);
        await _context.SaveChangesAsync();
        foreach (var attachment in attachments) files.DeleteFile(attachment.StorageKey);
        TempData["SuccessMessage"] = "Đã xóa CV khỏi hồ sơ.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadCv(string id)
    {
        var profile = await studentProfiles.GetProfileByUserIdAsync(id);
        if (profile == null) return NotFound();
        if (!await access.StudentAsync(User, profile)) return Forbid();
        var attachment = profile.Attachments.Where(a => a.FileCategory == FileCategories.StudentCv).OrderByDescending(a => a.UploadedAt).FirstOrDefault();
        if (attachment == null || !files.FileExists(attachment.StorageKey)) return NotFound();
        return PhysicalFile(files.GetFullPath(attachment.StorageKey), attachment.ContentType, attachment.OriginalFileName);
    }

    async Task<ProfileEditViewModel> ProfileModel()
    {
        var student = await studentProfiles.GetProfileByUserIdAsync(Actor);
        var lecturer = await _context.LecturerProfiles.FirstOrDefaultAsync(l => l.UserId == Actor);
        return new ProfileEditViewModel
        {
            IsStudent = User.IsInRole(AppRoles.Student), IsLecturer = User.IsInRole(AppRoles.Lecturer),
            ClassName = student?.Class?.Code, MajorId = student?.MajorId,
            MajorOptions = student?.Major == null ? new() : new() { new SelectListItem(student.Major.Name, student.MajorId.ToString()) },
            Skills = student?.Skills, Interests = student?.Interests,
            ExistingCvFileName = student?.Attachments.Where(a => a.FileCategory == FileCategories.StudentCv).OrderByDescending(a => a.UploadedAt).FirstOrDefault()?.OriginalFileName,
            LecturerSpecialty = lecturer?.Expertise
        };
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

        var lecturerRoleUsers = (await _userManager.GetUsersInRoleAsync(AppRoles.Lecturer)).Select(u => u.Id).ToList();
        var lecturers = (await _context.Users.Include(u => u.LecturerProfile).Where(u => lecturerRoleUsers.Contains(u.Id) && u.LecturerProfile != null && u.LecturerProfile.FacultyId == selectedCampaign.Major!.FacultyId).ToListAsync())
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .ToList();
        var studentRoleUsers = (await _userManager.GetUsersInRoleAsync(AppRoles.Student)).Select(u => u.Id).ToList();
        var students = (await _context.Users.Include(u => u.StudentProfile!).ThenInclude(s => s.Class).Where(u => studentRoleUsers.Contains(u.Id) && u.StudentProfile != null).ToListAsync())
            .Where(u => u.IsActive && u.StudentProfile!.MajorId == selectedCampaign.MajorId)
            .OrderBy(u => u.FullName)
            .ToList();
        var quotaRows = await _context.LecturerCampaignQuotas
            .Where(q => q.CampaignId == selectedCampaignId)
            .Include(q => q.LecturerProfile)
            .ToDictionaryAsync(q => q.LecturerProfile!.UserId);
        var counts = await _context.StudentAssignments.Where(a => a.CampaignId == selectedCampaignId)
            .GroupBy(a => a.LecturerProfileId).Select(g => new { Id = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count);
        var assignedCounts = lecturers.ToDictionary(u => u.Id, u => counts.GetValueOrDefault(u.LecturerProfile!.Id));
        var assignments = await _context.LecturerCampaignAssignments
            .Where(a => a.CampaignId == selectedCampaignId)
            .Include(a => a.Lecturer)
            .Include(a => a.Student)
            .Include(a => a.ReviewedByUser)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        // Show assignments made before this merge or through the other screen too.
        var actual = await (from a in _context.StudentAssignments
            join student in _context.StudentProfiles.Include(p => p.User) on a.StudentProfileId equals student.Id
            join lecturer in _context.LecturerProfiles.Include(p => p.User) on a.LecturerProfileId equals lecturer.Id
            where a.CampaignId == selectedCampaignId
            select new { Assignment = a, Student = student, Lecturer = lecturer }).ToListAsync();
        foreach (var row in actual)
        {
            if (assignments.Any(a => a.StudentUserId == row.Student.UserId && a.LecturerUserId == row.Lecturer.UserId && (a.Status == LecturerAssignmentStatuses.Assigned || a.Status == LecturerAssignmentStatuses.ApprovedException))) continue;
            assignments.Add(new LecturerCampaignAssignment { CampaignId = selectedCampaignId,
                StudentUserId = row.Student.UserId, Student = row.Student.User,
                LecturerUserId = row.Lecturer.UserId, Lecturer = row.Lecturer.User,
                Status = string.IsNullOrWhiteSpace(row.Assignment.QuotaOverrideReason) ? LecturerAssignmentStatuses.Assigned : LecturerAssignmentStatuses.ApprovedException,
                ExceptionReason = row.Assignment.QuotaOverrideReason,
                ReviewedByUserId = row.Assignment.AssignedBy, ReviewedAt = row.Assignment.AssignedAt,
                CreatedAt = row.Assignment.AssignedAt });
        }

        var cvUsers = await _context.FileAttachments.Where(a => a.FileCategory == FileCategories.StudentCv && a.StudentProfile != null)
            .Select(a => a.StudentProfile!.UserId).ToListAsync();
        var mentors = await _context.CompanyMentors.Where(m => _context.Companies.Any(c => c.Id == m.CompanyId && c.Status == "APPROVED" && c.MajorId == selectedCampaign.MajorId)).ToListAsync();
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
                    Specialty = lecturer.LecturerProfile!.Expertise ?? string.Empty,
                    QuotaId = quota?.Id,
                    Capacity = (int)(quota?.MaxStudents ?? 0),
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
                ClassName = s.StudentProfile!.Class?.Code ?? string.Empty,
                MajorName = selectedCampaign.Major?.Name ?? string.Empty,
                Skills = s.StudentProfile!.Skills ?? string.Empty,
                Interests = s.StudentProfile!.Interests ?? string.Empty,
                HasCv = cvUsers.Contains(s.Id)
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
            MentorOptions = mentors.Select(m => new SelectListItem(m.Name, m.Id.ToString())).ToList(),
            CanReviewExceptions = true
        });
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.TrainingManager},{AppRoles.FacultyManager}")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetQuota(SetLecturerQuotaViewModel model)
    {
        if (!await _scopeService.CanManageCampaignAsync(Actor, model.CampaignId)) return Forbid();
        var lecturer = await _context.LecturerProfiles.FirstOrDefaultAsync(l => l.UserId == model.LecturerUserId);
        var result = !ModelState.IsValid || lecturer == null
            ? (Success: false, ErrorMessage: (string?)"Thông tin quota không hợp lệ.")
            : await lecturerProfiles.SetQuotaAsync(lecturer.Id, model.CampaignId, (uint)model.Capacity, Actor);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success ? "Đã lưu quota giảng viên cho đợt." : result.ErrorMessage;
        return RedirectToAction(nameof(Manage), new { campaignId = model.CampaignId });
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.TrainingManager},{AppRoles.FacultyManager}")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAssignment(AddLecturerAssignmentViewModel model)
    {
        if (!await _scopeService.CanManageCampaignAsync(Actor, model.CampaignId)) return Forbid();
        var student = await _context.StudentProfiles.FirstOrDefaultAsync(s => s.UserId == model.StudentUserId);
        var lecturer = await _context.LecturerProfiles.FirstOrDefaultAsync(l => l.UserId == model.LecturerUserId);
        var result = !ModelState.IsValid || student == null || lecturer == null
            ? (Success: false, Message: "Thông tin phân công không hợp lệ hoặc tài khoản chưa có hồ sơ.")
            : await assignments.AssignAsync(Actor, model.CampaignId, student.Id, lecturer.Id, model.MentorId, model.ExceptionReason, requestException: true);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Manage), new { campaignId = model.CampaignId });
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.TrainingManager},{AppRoles.FacultyManager}")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewException(ulong id, bool approve)
    {
        var request = await _context.LecturerCampaignAssignments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
        if (request == null) return NotFound();
        if (!await _scopeService.CanManageCampaignAsync(Actor, request.CampaignId)) return Forbid();
        var result = await assignments.ReviewAsync(Actor, id, approve);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Manage), new { campaignId = request.CampaignId });
    }
}
