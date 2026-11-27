using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.ViewModels.InternshipRegistrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Student)]
public class InternshipRegistrationsController : Controller
{
    private static readonly string[] AllowedExtensions =
    {
        ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png"
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public InternshipRegistrationsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !user.IsActive)
            return Forbid();

        var registrations = await _context.InternshipRegistrations
            .AsNoTracking()
            .Include(r => r.Campaign)
            .Include(r => r.InternshipPlace)
            .Include(r => r.ReviewedByUser)
            .Where(r => r.StudentUserId == user.Id)
            .OrderByDescending(r => r.SubmittedAt)
            .ThenByDescending(r => r.Id)
            .ToListAsync();

        var openCampaigns = await GetEligibleInternshipCampaignsAsync(user);

        ViewBag.OpenCampaigns = openCampaigns;
        return View(registrations);
    }

    [HttpGet]
    public async Task<IActionResult> Create(ulong? campaignId = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !user.IsActive)
            return Forbid();

        var vm = new InternshipRegistrationFormViewModel
        {
            PlaceMode = "EXISTING"
        };

        if (campaignId.HasValue)
            vm.CampaignId = campaignId.Value;

        await PopulateFormOptionsAsync(vm, user);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InternshipRegistrationFormViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !user.IsActive)
            return Forbid();

        await PopulateFormOptionsAsync(model, user);

        var eligibleCampaigns = await GetEligibleInternshipCampaignsAsync(user);
        var campaign = eligibleCampaigns.FirstOrDefault(c => c.Id == model.CampaignId);
        if (campaign == null)
        {
            ModelState.AddModelError(
                nameof(model.CampaignId),
                "Đợt thực tập không hợp lệ, chưa mở hoặc đã hết hạn đăng ký.");
        }
        else
        {
            var hasActiveRegistration = await _context.InternshipRegistrations
                .AnyAsync(r => r.CampaignId == campaign.Id
                    && r.StudentUserId == user.Id
                    && (r.ApprovalStatus == InternshipRegistrationStatuses.Pending
                        || r.ApprovalStatus == InternshipRegistrationStatuses.Approved));

            if (hasActiveRegistration)
            {
                ModelState.AddModelError(
                    nameof(model.CampaignId),
                    "Bạn đã có hồ sơ đăng ký đang chờ duyệt hoặc đã được duyệt trong đợt thực tập này.");
            }
        }

        ValidateDateRange(model);
        var placeResult = await ResolveInternshipPlaceAsync(model, user.Id);
        if (!placeResult.Success)
        {
            foreach (var err in placeResult.Errors)
                ModelState.AddModelError(err.Key, err.Value);
        }

        FileUploadResult? uploadResult = null;
        if (model.ConfirmationFile != null && model.ConfirmationFile.Length > 0)
        {
            uploadResult = await SaveFileAsync(model.ConfirmationFile);
            if (!uploadResult.Success)
            {
                ModelState.AddModelError(
                    nameof(model.ConfirmationFile),
                    uploadResult.ErrorMessage ?? "Tệp đính kèm không hợp lệ.");
            }
        }

        if (!ModelState.IsValid || campaign == null || placeResult.Place == null)
        {
            if (uploadResult?.SavedFilePath != null && System.IO.File.Exists(uploadResult.SavedFilePath))
                System.IO.File.Delete(uploadResult.SavedFilePath);

            return View(model);
        }

        var now = DateTime.UtcNow;

        var registration = new InternshipRegistration
        {
            CampaignId = campaign.Id,
            StudentUserId = user.Id,
            InternshipPlaceId = placeResult.Place.Id,
            PositionTitle = string.IsNullOrWhiteSpace(model.PositionTitle)
                ? placeResult.Place.DefaultPosition
                : model.PositionTitle.Trim(),
            StartDate = model.StartDate?.Date,
            EndDate = model.EndDate?.Date,
            Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim(),
            OriginalFileName = uploadResult?.OriginalFileName,
            StoredFileName = uploadResult?.StoredFileName,
            ContentType = uploadResult?.ContentType,
            FileSize = uploadResult?.FileSize,
            ApprovalStatus = InternshipRegistrationStatuses.Pending,
            RejectionReason = null,
            ReviewedByUserId = null,
            ReviewedAt = null,
            SubmittedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.InternshipRegistrations.Add(registration);
        await _context.SaveChangesAsync();

        var reviewLog = new InternshipRegistrationReview
        {
            RegistrationId = registration.Id,
            AttemptNo = 1,
            Action = InternshipReviewActions.Submitted,
            Comment = "Sinh viên gửi hồ sơ đăng ký nơi thực tập.",
            PlaceNameSnapshot = placeResult.Place.Name,
            PositionSnapshot = registration.PositionTitle,
            ActorUserId = user.Id,
            CreatedAt = now
        };

        _context.InternshipRegistrationReviews.Add(reviewLog);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã gửi đăng ký nơi thực tập thành công và đang chờ cán bộ quản lý xét duyệt.";
        return RedirectToAction(nameof(Details), new { id = registration.Id });
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

        if (registration.StudentUserId != user.Id)
            return Forbid();

        var vm = BuildDetailViewModel(registration, isManagerView: false);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Resubmit(ulong id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !user.IsActive)
            return Forbid();

        var registration = await _context.InternshipRegistrations
            .AsNoTracking()
            .Include(r => r.Campaign)
            .Include(r => r.InternshipPlace)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (registration == null)
            return NotFound();

        if (registration.StudentUserId != user.Id)
            return Forbid();

        if (registration.ApprovalStatus != InternshipRegistrationStatuses.Rejected)
        {
            TempData["ErrorMessage"] = "Chỉ hồ sơ bị từ chối mới có thể chỉnh sửa và nộp lại.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var vm = new InternshipRegistrationFormViewModel
        {
            Id = registration.Id,
            CampaignId = registration.CampaignId,
            CampaignName = registration.Campaign?.Name,
            PlaceMode = "EXISTING",
            ExistingPlaceId = registration.InternshipPlaceId,
            PositionTitle = registration.PositionTitle,
            StartDate = registration.StartDate,
            EndDate = registration.EndDate,
            Note = registration.Note,
            ExistingOriginalFileName = registration.OriginalFileName,
            PreviousRejectionReason = registration.RejectionReason
        };

        await PopulateFormOptionsAsync(vm, user, includeCurrentCampaign: registration.Campaign);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resubmit(ulong id, InternshipRegistrationFormViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !user.IsActive)
            return Forbid();

        var registration = await _context.InternshipRegistrations
            .Include(r => r.Campaign)
            .Include(r => r.Reviews)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (registration == null)
            return NotFound();

        if (registration.StudentUserId != user.Id)
            return Forbid();

        if (registration.ApprovalStatus != InternshipRegistrationStatuses.Rejected)
        {
            TempData["ErrorMessage"] = "Chỉ hồ sơ bị từ chối mới có thể chỉnh sửa và nộp lại.";
            return RedirectToAction(nameof(Details), new { id });
        }

        model.Id = registration.Id;
        model.CampaignId = registration.CampaignId;
        model.CampaignName = registration.Campaign?.Name;
        model.PreviousRejectionReason = registration.RejectionReason;
        model.ExistingOriginalFileName = registration.OriginalFileName;

        await PopulateFormOptionsAsync(model, user, includeCurrentCampaign: registration.Campaign);

        ValidateDateRange(model);
        var placeResult = await ResolveInternshipPlaceAsync(model, user.Id);
        if (!placeResult.Success)
        {
            foreach (var err in placeResult.Errors)
                ModelState.AddModelError(err.Key, err.Value);
        }

        FileUploadResult? uploadResult = null;
        if (model.ConfirmationFile != null && model.ConfirmationFile.Length > 0)
        {
            uploadResult = await SaveFileAsync(model.ConfirmationFile);
            if (!uploadResult.Success)
            {
                ModelState.AddModelError(
                    nameof(model.ConfirmationFile),
                    uploadResult.ErrorMessage ?? "Tệp đính kèm không hợp lệ.");
            }
        }

        if (!ModelState.IsValid || placeResult.Place == null)
        {
            if (uploadResult?.SavedFilePath != null && System.IO.File.Exists(uploadResult.SavedFilePath))
                System.IO.File.Delete(uploadResult.SavedFilePath);

            return View(model);
        }

        var now = DateTime.UtcNow;
        var nextAttempt = (registration.Reviews.Any()
            ? registration.Reviews.Max(r => r.AttemptNo)
            : 0) + 1;

        registration.InternshipPlaceId = placeResult.Place.Id;
        registration.PositionTitle = string.IsNullOrWhiteSpace(model.PositionTitle)
            ? placeResult.Place.DefaultPosition
            : model.PositionTitle.Trim();
        registration.StartDate = model.StartDate?.Date;
        registration.EndDate = model.EndDate?.Date;
        registration.Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim();

        if (uploadResult != null && uploadResult.Success)
        {
            registration.OriginalFileName = uploadResult.OriginalFileName;
            registration.StoredFileName = uploadResult.StoredFileName;
            registration.ContentType = uploadResult.ContentType;
            registration.FileSize = uploadResult.FileSize;
        }

        registration.ApprovalStatus = InternshipRegistrationStatuses.Pending;
        registration.RejectionReason = null;
        registration.ReviewedByUserId = null;
        registration.ReviewedAt = null;
        registration.SubmittedAt = now;
        registration.UpdatedAt = now;

        _context.InternshipRegistrationReviews.Add(new InternshipRegistrationReview
        {
            RegistrationId = registration.Id,
            AttemptNo = nextAttempt,
            Action = InternshipReviewActions.Resubmitted,
            Comment = "Sinh viên cập nhật hồ sơ và gửi lại xét duyệt.",
            PlaceNameSnapshot = placeResult.Place.Name,
            PositionSnapshot = registration.PositionTitle,
            ActorUserId = user.Id,
            CreatedAt = now
        });

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã cập nhật và gửi lại hồ sơ đăng ký nơi thực tập thành công.";
        return RedirectToAction(nameof(Details), new { id = registration.Id });
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

        if (registration == null || registration.StudentUserId != user.Id)
            return Forbid();

        if (string.IsNullOrWhiteSpace(registration.StoredFileName)
            || string.IsNullOrWhiteSpace(registration.OriginalFileName))
            return NotFound();

        var uploadFolder = GetUploadFolder();
        var filePath = Path.Combine(uploadFolder, Path.GetFileName(registration.StoredFileName));
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var contentType = string.IsNullOrWhiteSpace(registration.ContentType)
            ? "application/octet-stream"
            : registration.ContentType;

        return PhysicalFile(filePath, contentType, registration.OriginalFileName);
    }

    private async Task<List<Campaign>> GetEligibleInternshipCampaignsAsync(ApplicationUser user)
    {
        var today = DateTime.Today;
        var query = _context.Campaigns
            .AsNoTracking()
            .Include(c => c.Major)
            .Include(c => c.Term)
            .Where(c => c.Kind == CampaignKinds.Internship
                && c.Status == CampaignStatuses.Open
                && c.StartDate.Date <= today
                && c.EndDate.Date >= today);

        if (user.MajorId.HasValue)
        {
            query = query.Where(c => c.MajorId == user.MajorId.Value);
        }

        return await query.OrderBy(c => c.Name).ToListAsync();
    }

    private async Task PopulateFormOptionsAsync(
        InternshipRegistrationFormViewModel vm,
        ApplicationUser user,
        Campaign? includeCurrentCampaign = null)
    {
        var campaigns = await GetEligibleInternshipCampaignsAsync(user);
        if (includeCurrentCampaign != null && campaigns.All(c => c.Id != includeCurrentCampaign.Id))
        {
            campaigns.Insert(0, includeCurrentCampaign);
        }

        vm.CampaignOptions = campaigns.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = $"{c.Name} ({c.Code})",
            Selected = c.Id == vm.CampaignId
        }).ToList();

        var places = await _context.InternshipPlaces
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();

        vm.ExistingPlaceOptions = places.Select(p => new SelectListItem
        {
            Value = p.Id.ToString(),
            Text = string.IsNullOrWhiteSpace(p.Address)
                ? p.Name
                : $"{p.Name} — {p.Address}",
            Selected = vm.ExistingPlaceId == p.Id
        }).ToList();
    }

    private static void ValidateDateRange(InternshipRegistrationFormViewModel model)
    {
        if (model.StartDate.HasValue && model.EndDate.HasValue
            && model.EndDate.Value.Date < model.StartDate.Value.Date)
        {
            model.ModelStateAddError(
                nameof(model.EndDate),
                "Ngày kết thúc thực tập không được nhỏ hơn ngày bắt đầu.");
        }
    }

    private async Task<(bool Success, InternshipPlace? Place, Dictionary<string, string> Errors)> ResolveInternshipPlaceAsync(
        InternshipRegistrationFormViewModel model,
        string userId)
    {
        var errors = new Dictionary<string, string>();
        var mode = (model.PlaceMode ?? "EXISTING").Trim().ToUpperInvariant();

        if (mode == "NEW")
        {
            if (string.IsNullOrWhiteSpace(model.NewPlaceName))
            {
                errors[nameof(model.NewPlaceName)] = "Vui lòng nhập tên doanh nghiệp / nơi thực tập.";
                return (false, null, errors);
            }

            var now = DateTime.UtcNow;
            var newPlace = new InternshipPlace
            {
                Name = model.NewPlaceName.Trim(),
                Address = string.IsNullOrWhiteSpace(model.NewPlaceAddress) ? null : model.NewPlaceAddress.Trim(),
                ContactPerson = string.IsNullOrWhiteSpace(model.NewPlaceContactPerson) ? null : model.NewPlaceContactPerson.Trim(),
                ContactPhone = string.IsNullOrWhiteSpace(model.NewPlaceContactPhone) ? null : model.NewPlaceContactPhone.Trim(),
                ContactEmail = string.IsNullOrWhiteSpace(model.NewPlaceContactEmail) ? null : model.NewPlaceContactEmail.Trim(),
                DefaultPosition = string.IsNullOrWhiteSpace(model.PositionTitle) ? null : model.PositionTitle.Trim(),
                IsActive = true,
                CreatedByUserId = userId,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.InternshipPlaces.Add(newPlace);
            await _context.SaveChangesAsync();
            return (true, newPlace, errors);
        }

        if (!model.ExistingPlaceId.HasValue || model.ExistingPlaceId.Value == 0)
        {
            errors[nameof(model.ExistingPlaceId)] = "Vui lòng chọn nơi thực tập có sẵn hoặc chuyển sang nhập nơi thực tập mới.";
            return (false, null, errors);
        }

        var existingPlace = await _context.InternshipPlaces
            .FirstOrDefaultAsync(p => p.Id == model.ExistingPlaceId.Value && p.IsActive);

        if (existingPlace == null)
        {
            errors[nameof(model.ExistingPlaceId)] = "Nơi thực tập đã chọn không tồn tại hoặc không còn hoạt động.";
            return (false, null, errors);
        }

        return (true, existingPlace, errors);
    }

    private string GetUploadFolder()
    {
        var folder = Path.Combine(
            _environment.WebRootPath
                ?? Path.Combine(_environment.ContentRootPath, "wwwroot"),
            "uploads",
            "internship-registrations");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        return folder;
    }

    private async Task<FileUploadResult> SaveFileAsync(IFormFile file)
    {
        if (file.Length > MaxFileSizeBytes)
        {
            return new FileUploadResult
            {
                Success = false,
                ErrorMessage = "Dung lượng tệp không được vượt quá 10 MB."
            };
        }

        var originalName = Path.GetFileName(file.FileName);
        var ext = Path.GetExtension(originalName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            return new FileUploadResult
            {
                Success = false,
                ErrorMessage = "Chỉ cho phép tải tệp định dạng PDF, DOC, DOCX, JPG, JPEG, PNG."
            };
        }

        var storedName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(GetUploadFolder(), storedName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return new FileUploadResult
        {
            Success = true,
            OriginalFileName = originalName,
            StoredFileName = storedName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            SavedFilePath = fullPath
        };
    }

    internal static InternshipRegistrationDetailViewModel BuildDetailViewModel(
        InternshipRegistration registration,
        bool isManagerView)
    {
        var studentName = registration.StudentUser?.FullName
            ?? registration.StudentUser?.UserName
            ?? "Không xác định";

        var reviewerName = registration.ReviewedByUser?.FullName
            ?? registration.ReviewedByUser?.UserName;

        var reviews = registration.Reviews
            .OrderByDescending(rv => rv.CreatedAt)
            .ThenByDescending(rv => rv.Id)
            .Select(rv => new InternshipReviewHistoryItemViewModel
            {
                AttemptNo = rv.AttemptNo,
                Action = rv.Action,
                Comment = rv.Comment,
                PlaceNameSnapshot = rv.PlaceNameSnapshot,
                PositionSnapshot = rv.PositionSnapshot,
                ActorName = rv.ActorUser?.FullName
                    ?? rv.ActorUser?.UserName
                    ?? "Không xác định",
                CreatedAt = rv.CreatedAt
            })
            .ToList();

        return new InternshipRegistrationDetailViewModel
        {
            Registration = registration,
            StudentName = studentName,
            StudentEmail = registration.StudentUser?.Email,
            ReviewerName = reviewerName,
            CanResubmit = !isManagerView
                && registration.ApprovalStatus == InternshipRegistrationStatuses.Rejected,
            CanApproveOrReject = isManagerView
                && registration.ApprovalStatus == InternshipRegistrationStatuses.Pending,
            Reviews = reviews
        };
    }

    private sealed class FileUploadResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? OriginalFileName { get; set; }
        public string? StoredFileName { get; set; }
        public string? ContentType { get; set; }
        public long FileSize { get; set; }
        public string? SavedFilePath { get; set; }
    }
}

internal static class InternshipFormModelStateExtensions
{
    public static void ModelStateAddError(
        this InternshipRegistrationFormViewModel _,
        string key,
        string message)
    {
        // Placeholder kept only if needed; handled directly via Controller ModelState.
    }
}