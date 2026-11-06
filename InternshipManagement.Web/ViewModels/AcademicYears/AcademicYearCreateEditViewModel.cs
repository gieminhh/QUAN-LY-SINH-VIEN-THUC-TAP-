
using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Web.ViewModels.AcademicYears;

public class AcademicYearCreateEditViewModel
{
    public ulong Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã năm học.")]
    [StringLength(15, ErrorMessage = "Mã năm học không được vượt quá 15 ký tự.")]
    [Display(Name = "Mã năm học")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu.")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày bắt đầu")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc.")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày kết thúc")]
    public DateTime EndDate { get; set; }
}
