using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using InternshipManagement.Web.Models;

namespace InternshipManagement.Web.ViewModels.Campaigns;

public class CampaignCreateEditViewModel : IValidatableObject
{
    public ulong Id { get; set; }

    [Required(ErrorMessage = "Mã đợt không được để trống.")]
    [StringLength(50, ErrorMessage = "Mã đợt tối đa 50 ký tự.")]
    [RegularExpression(@"^[a-zA-Z0-9_.-]+$", ErrorMessage = "Mã đợt chỉ chứa chữ, số, dấu gạch dưới hoặc gạch ngang.")]
    [Display(Name = "Mã đợt")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên đợt không được để trống.")]
    [StringLength(250, ErrorMessage = "Tên đợt tối đa 250 ký tự.")]
    [Display(Name = "Tên đợt")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn học kỳ.")]
    [Display(Name = "Học kỳ & Năm học")]
    public ulong TermId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngành đào tạo.")]
    [Display(Name = "Ngành đào tạo")]
    public ulong MajorId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại đợt.")]
    [Display(Name = "Loại đợt")]
    public string Kind { get; set; } = CampaignKinds.Project;

    [Required(ErrorMessage = "Hệ đào tạo không được để trống.")]
    [StringLength(80)]
    [Display(Name = "Hệ đào tạo")]
    public string TrainingSystem { get; set; } = "CHINH_QUY";

    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu.")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày bắt đầu")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc.")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày kết thúc")]
    public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(3);

    [Display(Name = "Trạng thái đợt")]
    public string Status { get; set; } = CampaignStatuses.Draft;

    // Dropdown lists
    public List<SelectListItem> TermOptions { get; set; } = new();
    public List<SelectListItem> MajorOptions { get; set; } = new();
    public List<SelectListItem> KindOptions { get; set; } = new();
    public List<SelectListItem> StatusOptions { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate < StartDate)
        {
            yield return new ValidationResult(
                "Ngày kết thúc đợt không được trước ngày bắt đầu.",
                new[] { nameof(EndDate) }
            );
        }
    }
}
