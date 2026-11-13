using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

public static class TopicProposalStatuses
{
    public const string Pending = "PENDING";
    public const string Approved = "APPROVED";
    public const string RevisionRequested = "REVISION_REQUESTED";
    public const string Rejected = "REJECTED";
}

[Table("topic_proposals")]
public class TopicProposal
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("campaign_id")]
    public ulong CampaignId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("student_user_id")]
    public string StudentUserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("lecturer_user_id")]
    public string LecturerUserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column("description", TypeName = "longtext")]
    public string Description { get; set; } = string.Empty;

    [MaxLength(1000)]
    [Column("objectives", TypeName = "longtext")]
    public string? Objectives { get; set; }

    [MaxLength(1000)]
    [Column("student_note", TypeName = "longtext")]
    public string? StudentNote { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("status")]
    public string Status { get; set; } = TopicProposalStatuses.Pending;

    [MaxLength(2000)]
    [Column("review_feedback", TypeName = "longtext")]
    public string? ReviewFeedback { get; set; }

    [Column("reviewed_at")]
    public DateTime? ReviewedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CampaignId))]
    public virtual Campaign? Campaign { get; set; }

    [ForeignKey(nameof(StudentUserId))]
    public virtual ApplicationUser? Student { get; set; }

    [ForeignKey(nameof(LecturerUserId))]
    public virtual ApplicationUser? Lecturer { get; set; }
}
