using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InternshipManagement.Web.ViewModels.TopicProposals;

public class TopicProposalCreateViewModel
{
    [Display(Name = "Đợt đào tạo / giảng viên")]
    [Range(typeof(ulong), "1", "18446744073709551615", ErrorMessage = "Vui lòng chọn giảng viên được phân công.")]
    public ulong AssignmentId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên đề tài.")]
    [StringLength(250, ErrorMessage = "Tên đề tài không được vượt quá 250 ký tự.")]
    [Display(Name = "Tên đề tài")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng mô tả đề tài.")]
    [Display(Name = "Mô tả")]
    public string Description { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Mục tiêu không được vượt quá 1000 ký tự.")]
    [Display(Name = "Mục tiêu")]
    public string? Objectives { get; set; }

    [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
    [Display(Name = "Ghi chú gửi giảng viên")]
    public string? StudentNote { get; set; }

    public List<SelectListItem> AssignmentOptions { get; set; } = new();
}
