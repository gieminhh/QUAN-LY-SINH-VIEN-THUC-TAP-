using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InternshipManagement.Web.ViewModels.Topics;

public class TopicCopyViewModel
{
    [Required]
    public ulong SourceTopicId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn đợt đào tạo đích.")]
    [Display(Name = "Đợt đào tạo mới")]
    public ulong TargetCampaignId { get; set; }

    public string SourceTopicTitle { get; set; } = string.Empty;

    public string SourceCampaignName { get; set; } = string.Empty;

    public List<SelectListItem> CampaignOptions { get; set; } = new();
}