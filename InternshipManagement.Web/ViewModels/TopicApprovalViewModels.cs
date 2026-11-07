using System.ComponentModel.DataAnnotations;
using InternshipManagement.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InternshipManagement.Web.ViewModels;

public class TopicApprovalListViewModel
{
    public List<Topic> Topics { get; set; } = new();

    public ulong? CampaignId { get; set; }
    public string? Status { get; set; } = TopicStatuses.Pending; // Default to pending review
    public ulong? MajorId { get; set; }
    public string? SearchQuery { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

    public SelectList? CampaignOptions { get; set; }
    public SelectList? StatusOptions { get; set; }
    public SelectList? MajorOptions { get; set; }
}

public class TopicReviewViewModel
{
    public Topic Topic { get; set; } = null!;
    public List<TopicApprovalHistory> Histories { get; set; } = new();
    public FileAttachment? DocumentAttachment { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nhận xét / lý do")]
    [Display(Name = "Ý kiến nhận xét / Lý do")]
    public string CommentText { get; set; } = string.Empty;

    public bool CanReview { get; set; }
}
