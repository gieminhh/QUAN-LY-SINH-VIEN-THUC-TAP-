namespace InternshipManagement.Web.ViewModels.Topics;

public class TopicDetailsViewModel
{
    public ulong Id { get; set; }

    public ulong CampaignId { get; set; }

    public string CampaignName { get; set; } = string.Empty;

    public string CampaignKind { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? Objectives { get; set; }

    public string? Specialty { get; set; }

    public string? Technologies { get; set; }

    public string? Requirements { get; set; }

    public string TopicType { get; set; } = string.Empty;

    public int? MaxStudents { get; set; }

    public int? MaxGroups { get; set; }

    public string Status { get; set; } = string.Empty;

    public string CreatedByName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ulong? SourceTopicId { get; set; }

    public string? SourceTopicTitle { get; set; }

    public List<TopicAttachmentViewModel> Attachments { get; set; } = new();

    public bool CanEdit { get; set; }

    public bool CanCopy { get; set; }
}

public class TopicAttachmentViewModel
{
    public ulong Id { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public string? ContentType { get; set; }

    public string UploadedByName { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; }
}