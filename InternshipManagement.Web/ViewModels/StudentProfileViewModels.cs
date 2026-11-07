using System.ComponentModel.DataAnnotations;
using InternshipManagement.Web.Models;

namespace InternshipManagement.Web.ViewModels;

public class StudentProfileViewModel
{
    public ulong Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    public string ClassName { get; set; } = "Chưa xếp lớp";
    public string MajorName { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;
    public string? Cohort { get; set; }

    // GPA
    public decimal? LatestGpa4 { get; set; }
    public decimal? LatestGpa10 { get; set; }
    public string? LatestTermName { get; set; }
    public List<StudentGpaRecord> GpaHistory { get; set; } = new();

    // Editable by student
    [Display(Name = "Kỹ năng chuyên môn")]
    public string? Skills { get; set; }

    [Display(Name = "Lĩnh vực quan tâm")]
    public string? Interests { get; set; }

    // CV
    public FileAttachment? CvAttachment { get; set; }

    public bool CanEdit { get; set; } = true;
}

public class StudentProfileEditViewModel
{
    public ulong Id { get; set; }

    // Readonly display fields for confirmation (nullable so model validation ignores them)
    public string? FullName { get; set; }
    public string? StudentCode { get; set; }
    public string? ClassName { get; set; }
    public string? MajorName { get; set; }
    public decimal? LatestGpa4 { get; set; }
    public decimal? LatestGpa10 { get; set; }

    // Editable fields
    [Display(Name = "Kỹ năng chuyên môn")]
    public string? Skills { get; set; }

    [Display(Name = "Lĩnh vực quan tâm")]
    public string? Interests { get; set; }

    [Display(Name = "Tải lên CV (PDF, DOC, DOCX)")]
    public IFormFile? CvFile { get; set; }

    public FileAttachment? CurrentCvAttachment { get; set; }
}
