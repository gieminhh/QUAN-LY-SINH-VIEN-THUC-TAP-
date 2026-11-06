using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InternshipManagement.Web.ViewModels.Majors;

public class MajorCreateEditViewModel
{
    public ulong Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn khoa.")]
    [Range(typeof(ulong), "1", "18446744073709551615",
        ErrorMessage = "Vui lòng chọn khoa hợp lệ.")]
    [Display(Name = "Khoa trực thuộc")]
    public ulong FacultyId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã ngành.")]
    [StringLength(30, ErrorMessage = "Mã ngành không được vượt quá 30 ký tự.")]
    [Display(Name = "Mã ngành")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên ngành.")]
    [StringLength(200, ErrorMessage = "Tên ngành không được vượt quá 200 ký tự.")]
    [Display(Name = "Tên ngành")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Trạng thái hoạt động")]
    public bool IsActive { get; set; } = true;

    public List<SelectListItem> Faculties { get; set; } = new();
}