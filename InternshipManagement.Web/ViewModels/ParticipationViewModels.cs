using System.ComponentModel.DataAnnotations;
using InternshipManagement.Web.Models;
namespace InternshipManagement.Web.ViewModels;
public sealed class PolicyPage
{
    public List<Campaign> Campaigns { get; set; } = [];
    public ParticipationPolicy Input { get; set; } = new();
}
public sealed class GroupPage
{
    public List<Campaign> Campaigns { get; set; } = [];
    public ulong CampaignId { get; set; }
    public ParticipationPolicy? Policy { get; set; }
    public CampaignParticipant? Participation { get; set; }
    public ProjectRegistrationGroup? Group { get; set; }
    public List<ApplicationUser> Members { get; set; } = [];
    public List<ApplicationUser> Invitees { get; set; } = [];
    public List<ProjectRegistrationGroup> Invitations { get; set; } = [];
    public List<ProjectGroupInvitation> SentInvitations { get; set; } = [];
    public string? LockedReason { get; set; }
    public bool IsLeader { get; set; }
    public bool CanGroup { get; set; }
}
public sealed class RankingInput
{
    public ulong TopicId { get; set; }
    [RegularExpression("^[0-9a-f]{32}$")] public string? Version { get; set; }
    [Required, MaxLength(1000)] public string Reason { get; set; } = "";
    public List<string> StudentIds { get; set; } = [];
}
public sealed class RankingPage
{
    public List<Topic> Topics { get; set; } = [];
    public RankingInput Input { get; set; } = new();
    public List<ApplicationUser> Candidates { get; set; } = [];
    public string? LockedReason { get; set; }
    public DateTime? SavedAt { get; set; }
}
