using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace InternshipManagement.Web.Models;

[Table("campaign_participation_policies")]
public sealed class ParticipationPolicy
{
    [Key] public ulong CampaignId { get; set; }
    public bool GroupsAllowed { get; set; }
    [Range(2, 20)] public int MaxGroupSize { get; set; } = 5;
    public DateTime RegistrationOpensAt { get; set; }
    public DateTime RegistrationClosesAt { get; set; }
    public bool LecturerRankingEnabled { get; set; }
    public DateTime RankingOpensAt { get; set; }
    public DateTime RankingClosesAt { get; set; }
}
[Table("project_registration_groups")]
public sealed class ProjectRegistrationGroup
{
    public ulong Id { get; set; }
    public ulong CampaignId { get; set; }
    [Required, MaxLength(255)] public string LeaderId { get; set; } = "";
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
[Table("campaign_participants")]
public sealed class CampaignParticipant
{
    public ulong CampaignId { get; set; }
    [Required, MaxLength(255)] public string StudentId { get; set; } = "";
    public ulong? GroupId { get; set; }
    public DateTime ConfirmedAt { get; set; } = DateTime.UtcNow;
}
[Table("project_group_invitations")]
public sealed class ProjectGroupInvitation
{
    public ulong GroupId { get; set; }
    [Required, MaxLength(255)] public string StudentId { get; set; } = "";
    [MaxLength(20)] public string Status { get; set; } = "PENDING";
    public DateTime InvitedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
}
[Table("lecturer_priority_lists")]
public sealed class LecturerPriorityList
{
    public ulong TopicId { get; set; }
    [Required, MaxLength(255)] public string LecturerId { get; set; } = "";
    [Required, MaxLength(32)] public string Version { get; set; } = "";
    [Required, MaxLength(1000)] public string Reason { get; set; } = "";
    public DateTime SavedAt { get; set; }
}
[Table("lecturer_student_priorities")]
public sealed class LecturerStudentPriority
{
    public ulong TopicId { get; set; }
    [Required, MaxLength(255)] public string LecturerId { get; set; } = "";
    [Required, MaxLength(255)] public string StudentId { get; set; } = "";
    public int Rank { get; set; }
}
