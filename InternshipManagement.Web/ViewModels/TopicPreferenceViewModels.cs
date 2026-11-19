using System.ComponentModel.DataAnnotations;
using InternshipManagement.Web.Models;

namespace InternshipManagement.Web.ViewModels;

public sealed class TopicPreferenceInput
{
    public ulong CampaignId { get; set; }

    [RegularExpression("^[0-9a-f]{32}$")]
    public string? Version { get; set; }

    [MaxLength(180, ErrorMessage = "Hướng quan tâm tối đa 180 ký tự.")]
    public string? ResearchDirection { get; set; }

    public List<ulong?> TopicIds { get; set; } = [];
}

public sealed class TopicPreferencePage
{
    public List<Campaign> Campaigns { get; init; } = [];
    public TopicPreferenceInput Input { get; set; } = new();
    public List<Topic> Topics { get; init; } = [];
    public List<TopicPreference> SavedChoices { get; init; } = [];
    public DateTime? SavedAtUtc { get; init; }
    public DateTime? ClosesAt { get; init; }
    public string? LockedReason { get; init; }
}
