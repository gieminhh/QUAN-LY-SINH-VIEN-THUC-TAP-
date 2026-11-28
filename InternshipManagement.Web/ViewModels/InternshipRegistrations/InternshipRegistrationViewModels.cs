using System.ComponentModel.DataAnnotations;
using InternshipManagement.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InternshipManagement.Web.ViewModels.InternshipRegistrations;

public class InternshipRegistrationFormViewModel
{
    public ulong? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn đợt thực tập.")]
    [Display(Name = "Đợt thực tập")]
    public ulong CampaignId { get; set; }

    /// <summary>
    /// "EXISTING" = chọn nơi thực tập có sẵn; "NEW" = tự nhập nơi thực tập mới
    /// </summary>
    [Required]
    [Display(Name = "Hình thức chọn nơi thực tập")]
    public string PlaceMode { get; set; } = "EXISTING";

    [Display(Name = "Nơi thực tập có sẵn")]
    public ulong? ExistingPlaceId { get; set; }

    [MaxLength(250, ErrorMessage = "Tên nơi thực tập tối đa 250 ký tự.")]
    [Display(Name = "Tên doanh nghiệp / nơi thực tập")]
    public string? NewPlaceName { get; set; }

    [MaxLength(500, ErrorMessage = "Địa chỉ tối đa 500 ký tự.")]
    [Display(Name = "Địa chỉ")]
    public string? NewPlaceAddress { get; set; }

    [MaxLength(180, ErrorMessage = "Người liên hệ tối đa 180 ký tự.")]
    [Display(Name = "Người liên hệ / hướng dẫn tại đơn vị")]
    public string? NewPlaceContactPerson { get; set; }

    [MaxLength(50, ErrorMessage = "Số điện thoại tối đa 50 ký tự.")]
    [Display(Name = "Số điện thoại liên hệ")]
    public string? NewPlaceContactPhone { get; set; }

    [MaxLength(180, ErrorMessage = "Email tối đa 180 ký tự.")]
    [EmailAddress(ErrorMessage = "Email liên hệ không hợp lệ.")]
    [Display(Name = "Email liên hệ")]
    public string? NewPlaceContactEmail { get; set; }

    [MaxLength(200, ErrorMessage = "Vị trí thực tập tối đa 200 ký tự.")]
    [Display(Name = "Vị trí thực tập")]
    public string? PositionTitle { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày bắt đầu dự kiến")]
    public DateTime? StartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày kết thúc dự kiến")]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Ghi chú / kế hoạch thực tập")]
    public string? Note { get; set; }

    [Display(Name = "Giấy tiếp nhận / file minh chứng (PDF, DOC, DOCX, JPG, PNG - tối đa 10MB)")]
    public IFormFile? ConfirmationFile { get; set; }

    // Dữ liệu hỗ trợ hiển thị lại khi nộp lại / validate
    public string? ExistingOriginalFileName { get; set; }
    public string? PreviousRejectionReason { get; set; }
    public string? CampaignName { get; set; }

    public List<SelectListItem> CampaignOptions { get; set; } = new();
    public List<SelectListItem> ExistingPlaceOptions { get; set; } = new();
}

public class InternshipReviewHistoryItemViewModel
{
    public int AttemptNo { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public string? PlaceNameSnapshot { get; set; }
    public string? PositionSnapshot { get; set; }
    public string ActorName { get; set; } = "Không xác định";
    public DateTime CreatedAt { get; set; }
}

public class InternshipRegistrationDetailViewModel
{
    public InternshipRegistration Registration { get; set; } = null!;
    public string StudentName { get; set; } = "Không xác định";
    public string? StudentEmail { get; set; }
    public string? ReviewerName { get; set; }
    public bool CanResubmit { get; set; }
    public bool CanApproveOrReject { get; set; }
    public List<InternshipReviewHistoryItemViewModel> Reviews { get; set; } = new();
}

public class InternshipApprovalListItemViewModel
{
    public ulong Id { get; set; }
    public ulong CampaignId { get; set; }
    public string CampaignName { get; set; } = "Không xác định";
    public string StudentName { get; set; } = "Không xác định";
    public string PlaceName { get; set; } = "Không xác định";
    public string? PositionTitle { get; set; }
    public string ApprovalStatus { get; set; } = InternshipRegistrationStatuses.Pending;
    public string? RejectionReason { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string? ReviewerName { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public class InternshipApprovalListViewModel
{
    public string? Search { get; set; }
    public ulong? CampaignFilter { get; set; }
    public string ApprovalFilter { get; set; } = "ALL";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;

    public List<SelectListItem> CampaignOptions { get; set; } = new();
    public List<InternshipApprovalListItemViewModel> Registrations { get; set; } = new();
}