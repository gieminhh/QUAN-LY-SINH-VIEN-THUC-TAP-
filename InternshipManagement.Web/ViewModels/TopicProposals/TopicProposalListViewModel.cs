namespace InternshipManagement.Web.ViewModels.TopicProposals;

public class TopicProposalListViewModel
{
    public bool IsLecturer { get; set; }
    public List<TopicProposalListItemViewModel> Proposals { get; set; } = new();
}

public class TopicProposalListItemViewModel
{
    public ulong Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Objectives { get; set; }
    public string? StudentNote { get; set; }
    public string? ReviewFeedback { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string LecturerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
