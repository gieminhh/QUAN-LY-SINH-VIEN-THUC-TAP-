using System.ComponentModel.DataAnnotations;
using InternshipManagement.Web.Models;

namespace InternshipManagement.Web.ViewModels;

public class LecturerProfileViewModel
{
    public ulong Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string LecturerCode { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;

    public string? DepartmentName { get; set; }
    public string? AcademicDegree { get; set; }
    public string? AcademicTitle { get; set; }
    public string? Expertise { get; set; }
    public string? ResearchInterests { get; set; }

    public List<LecturerCampaignQuota> Quotas { get; set; } = new();
    public List<Topic> Topics { get; set; } = new();

    public bool CanEdit { get; set; } = true;
}

public class LecturerProfileEditViewModel
{
    public ulong Id { get; set; }

    // Read-only for display (nullable so model validation ignores them during POST)
    public string? FullName { get; set; }
    public string? LecturerCode { get; set; }
    public string? FacultyName { get; set; }

    // Editable fields
    [Display(Name = "Bộ môn / Đơn vị trực thuộc")]
    [MaxLength(180, ErrorMessage = "Tên bộ môn không được vượt quá 180 ký tự")]
    public string? DepartmentName { get; set; }

    [Display(Name = "Học vị (Thạc sĩ, Tiến sĩ...)")]
    [MaxLength(100, ErrorMessage = "Học vị không được vượt quá 100 ký tự")]
    public string? AcademicDegree { get; set; }

    [Display(Name = "Học hàm (Phó Giáo sư, Giáo sư...)")]
    [MaxLength(100, ErrorMessage = "Học hàm không được vượt quá 100 ký tự")]
    public string? AcademicTitle { get; set; }

    [Display(Name = "Chuyên môn")]
    public string? Expertise { get; set; }

    [Display(Name = "Hướng nghiên cứu")]
    public string? ResearchInterests { get; set; }
}
