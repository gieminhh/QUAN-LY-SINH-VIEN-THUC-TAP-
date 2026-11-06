
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InternshipManagement.Web.ViewModels.Terms;

public class TermCreateEditViewModel
{
    public ulong Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn năm học.")]
    [Range(1, ulong.MaxValue, ErrorMessage = "Vui lòng chọn năm học.")]
    [Display(Name = "Năm học")]
    public ulong AcademicYearId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn học kỳ.")]
    [Range(1, 2, ErrorMessage = "Số học kỳ chỉ được là 1 hoặc 2.")]
    [Display(Name = "Học kỳ")]
    public byte TermNo { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu.")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày bắt đầu")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc.")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày kết thúc")]
    public DateTime EndDate { get; set; }

    public List<SelectListItem> AcademicYears { get; set; } = new();
}
