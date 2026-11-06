using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Web.ViewModels.Faculties;

public class FacultyCreateEditViewModel
{
    public ulong Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã khoa.")]
    [StringLength(30, ErrorMessage = "Mã khoa không được vượt quá 30 ký tự.")]
    [Display(Name = "Mã khoa")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên khoa.")]
    [StringLength(200, ErrorMessage = "Tên khoa không được vượt quá 200 ký tự.")]
    [Display(Name = "Tên khoa")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Trạng thái hoạt động")]
    public bool IsActive { get; set; } = true;
}