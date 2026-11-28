using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

public enum MatchingRoundState { RegistrationOpen = 0, Published = 1 }

[Table("matching_rounds")]
public sealed class MatchingRound
{
    public ulong Id { get; set; }
    public ulong CampaignId { get; set; }
    public int Number { get; set; }
    public MatchingRoundState State { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosesAt { get; set; }
    [Required, MaxLength(255)] public string OpenedBy { get; set; } = "";
    public DateTime? PublishedAt { get; set; }
    [MaxLength(255)] public string? PublishedBy { get; set; }
}

// Immutable published pair. Student confirmation only sets ConfirmedAt.
[Table("matching_published_results")]
public sealed class MatchingPublishedResult
{
    public ulong RoundId { get; set; }
    [Required, MaxLength(255)] public string StudentId { get; set; } = "";
    public ulong? TopicId { get; set; }
    [MaxLength(250)] public string? TopicTitle { get; set; }
    public MatchStatus Status { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}

[Table("matching_round_preferences")]
public sealed class MatchingRoundPreference
{
    public ulong RoundId { get; set; }
    [Required, MaxLength(255)] public string StudentId { get; set; } = "";
    public ulong TopicId { get; set; }
    public int Priority { get; set; }
    public DateTime SubmittedAt { get; set; }
}
