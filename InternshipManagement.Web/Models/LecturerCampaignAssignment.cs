using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

public static class LecturerAssignmentStatuses
{
    public const string Assigned = "ASSIGNED";
    public const string PendingException = "PENDING_EXCEPTION";
    public const string ApprovedException = "APPROVED_EXCEPTION";
    public const string RejectedException = "REJECTED_EXCEPTION";
}

[Table("lecturer_campaign_assignments")]
public class LecturerCampaignAssignment
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("campaign_id")]
    public ulong CampaignId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("lecturer_user_id")]
    public string LecturerUserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("student_user_id")]
    public string StudentUserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    [Column("status")]
    public string Status { get; set; } = LecturerAssignmentStatuses.Assigned;

    [MaxLength(1000)]
    [Column("exception_reason")]
    public string? ExceptionReason { get; set; }

    [MaxLength(255)]
    [Column("reviewed_by_user_id")]
    public string? ReviewedByUserId { get; set; }

    [Column("reviewed_at")]
    public DateTime? ReviewedAt { get; set; }

    [Column("company_mentor_id")]
    public ulong? CompanyMentorId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CampaignId))]
    public virtual Campaign? Campaign { get; set; }

    [ForeignKey(nameof(LecturerUserId))]
    public virtual ApplicationUser? Lecturer { get; set; }

    [ForeignKey(nameof(StudentUserId))]
    public virtual ApplicationUser? Student { get; set; }

    [ForeignKey(nameof(ReviewedByUserId))]
    public virtual ApplicationUser? ReviewedByUser { get; set; }
}
