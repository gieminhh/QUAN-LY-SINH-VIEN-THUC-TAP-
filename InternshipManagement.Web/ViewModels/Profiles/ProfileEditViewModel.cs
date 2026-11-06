using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InternshipManagement.Web.ViewModels.Profiles;

public class ProfileEditViewModel
{
    [Display(Name = "Lớp")]
    [StringLength(100)]
    public string? ClassName { get; set; }

    [Display(Name = "Ngành")]
    public ulong? MajorId { get; set; }

    public List<SelectListItem> MajorOptions { get; set; } = new();

    [Display(Name = "Kỹ năng")]
    [StringLength(2000)]
    public string? Skills { get; set; }

    [Display(Name = "Sở thích")]
    [StringLength(2000)]
    public string? Interests { get; set; }

    [Display(Name = "CV (PDF, tối đa 5 MB)")]
    public IFormFile? CvFile { get; set; }

    public string? ExistingCvFileName { get; set; }

    [Display(Name = "Chuyên môn")]
    [StringLength(1000)]
    public string? LecturerSpecialty { get; set; }

    public bool IsStudent { get; set; }
    public bool IsLecturer { get; set; }
}
