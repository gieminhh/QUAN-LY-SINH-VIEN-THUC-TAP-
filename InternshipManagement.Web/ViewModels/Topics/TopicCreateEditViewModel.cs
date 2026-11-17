using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using InternshipManagement.Web.Models;

namespace InternshipManagement.Web.ViewModels.Topics;

public class TopicCreateEditViewModel
{
    public ulong? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn đợt đào tạo.")]
    [Display(Name = "Đợt đào tạo")]
    public ulong CampaignId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên đề tài.")]
    [StringLength(250, ErrorMessage = "Tên đề tài không được vượt quá 250 ký tự.")]
    [Display(Name = "Tên đề tài")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mô tả đề tài.")]
    [Display(Name = "Mô tả đề tài")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Mục tiêu")]
    public string? Objectives { get; set; }

    [StringLength(1000)]
    [Display(Name = "Chuyên ngành")]
    public string? Specialty { get; set; }

    [StringLength(1000)]
    [Display(Name = "Công nghệ sử dụng")]
    public string? Technologies { get; set; }

    [Display(Name = "Yêu cầu")]
    public string? Requirements { get; set; }

    [Required]
    [Display(Name = "Hình thức đề tài")]
    public string TopicType { get; set; } = TopicTypes.Individual;

    [Range(1, 1000, ErrorMessage = "Số lượng sinh viên phải từ 1 đến 1000.")]
    [Display(Name = "Số sinh viên tối đa")]
    public int? MaxStudents { get; set; }

    [Range(1, 1000, ErrorMessage = "Số lượng nhóm phải từ 1 đến 1000.")]
    [Display(Name = "Số nhóm tối đa")]
    public int? MaxGroups { get; set; }

    [Required]
    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = TopicStatuses.Published;

    [Display(Name = "Tệp đính kèm")]
    public List<IFormFile> Files { get; set; } = new();

    public List<SelectListItem> CampaignOptions { get; set; } = new();

    public List<SelectListItem> TopicTypeOptions { get; set; } = new();

    public List<SelectListItem> StatusOptions { get; set; } = new();

    public List<TopicAttachmentViewModel> ExistingAttachments { get; set; } = new();

    // Xác nhận đã xem cảnh báo và vẫn muốn tiếp tục
public bool ConfirmSimilarTopics { get; set; }

// Danh sách đề tài có nội dung tương đồng
public List<SimilarTopicWarningViewModel> SimilarTopics { get; set; } = new();
}

public class SimilarTopicWarningViewModel
{
    public ulong Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string CampaignName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public double SimilarityPercent { get; set; }
}
