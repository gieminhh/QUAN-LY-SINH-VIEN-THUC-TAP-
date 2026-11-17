using Microsoft.AspNetCore.Mvc.Rendering;

namespace InternshipManagement.Web.ViewModels.TopicApprovals;

public class TopicApprovalListViewModel
{
    public string? Search { get; set; }

    public ulong? CampaignFilter { get; set; }

    public string ApprovalFilter { get; set; } = "ALL";

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalCount { get; set; }

    public int TotalPages =>
        (int)Math.Ceiling((double)TotalCount / PageSize);

    public List<SelectListItem> CampaignOptions { get; set; } = new();

    public List<TopicApprovalListItemViewModel> Topics { get; set; } = new();
}

public class TopicApprovalListItemViewModel
{
    public ulong Id { get; set; }

    public ulong CampaignId { get; set; }

    public string CampaignName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string TopicType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string ApprovalStatus { get; set; } = string.Empty;

    public string CreatedByName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string? ReviewedByUserId { get; set; }

    public string ReviewerName { get; set; } = string.Empty;

    public DateTime? ReviewedAt { get; set; }

    public string? RejectionReason { get; set; }
}
