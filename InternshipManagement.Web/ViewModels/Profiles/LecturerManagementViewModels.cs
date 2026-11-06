using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using InternshipManagement.Web.Models;

namespace InternshipManagement.Web.ViewModels.Profiles;

public class LecturerManagementViewModel
{
    public ulong CampaignId { get; set; }
    public List<SelectListItem> CampaignOptions { get; set; } = new();
    public List<LecturerQuotaRowViewModel> Quotas { get; set; } = new();
    public List<SelectListItem> LecturerOptions { get; set; } = new();
    public List<SelectListItem> StudentOptions { get; set; } = new();
    public List<StudentProfileRowViewModel> StudentProfiles { get; set; } = new();
    public List<LecturerAssignmentRowViewModel> Assignments { get; set; } = new();
    public bool CanReviewExceptions { get; set; }
}

public class StudentProfileRowViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string MajorName { get; set; } = string.Empty;
    public string Skills { get; set; } = string.Empty;
    public string Interests { get; set; } = string.Empty;
    public bool HasCv { get; set; }
}

public class LecturerQuotaRowViewModel
{
    public string LecturerUserId { get; set; } = string.Empty;
    public string LecturerName { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public ulong? QuotaId { get; set; }
    public int Capacity { get; set; }
    public int AssignedCount { get; set; }
}

public class LecturerAssignmentRowViewModel
{
    public ulong Id { get; set; }
    public string LecturerName { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ExceptionReason { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public bool CanReview { get; set; }
}

public class SetLecturerQuotaViewModel
{
    [Range(1, ulong.MaxValue)]
    public ulong CampaignId { get; set; }

    [Required]
    public string LecturerUserId { get; set; } = string.Empty;

    [Range(0, 1000, ErrorMessage = "Quota phải từ 0 đến 1000 sinh viên.")]
    public int Capacity { get; set; }
}

public class AddLecturerAssignmentViewModel
{
    [Range(1, ulong.MaxValue)]
    public ulong CampaignId { get; set; }

    [Required]
    public string LecturerUserId { get; set; } = string.Empty;

    [Required]
    public string StudentUserId { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Lý do ngoại lệ tối đa 1000 ký tự.")]
    public string? ExceptionReason { get; set; }
}
