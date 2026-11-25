using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

[Table("internship_registrations")]
public class InternshipRegistration
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

    [Column("internship_place_id")]
    public ulong InternshipPlaceId { get; set; }

    [MaxLength(200)]
    [Column("position_title")]
    public string? PositionTitle { get; set; }

    [Column("start_date", TypeName = "date")]
    public DateTime? StartDate { get; set; }

    [Column("end_date", TypeName = "date")]
    public DateTime? EndDate { get; set; }

    [Column("note", TypeName = "longtext")]
    public string? Note { get; set; }

    [MaxLength(255)]
    [Column("original_file_name")]
    public string? OriginalFileName { get; set; }

    [MaxLength(255)]
    [Column("stored_file_name")]
    public string? StoredFileName { get; set; }

    [MaxLength(120)]
    [Column("content_type")]
    public string? ContentType { get; set; }

    [Column("file_size")]
    public long? FileSize { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("approval_status")]
    public string ApprovalStatus { get; set; } = InternshipRegistrationStatuses.Pending;

    [Column("rejection_reason", TypeName = "longtext")]
    public string? RejectionReason { get; set; }

    [MaxLength(255)]
    [Column("reviewed_by_user_id")]
    public string? ReviewedByUserId { get; set; }

    [Column("reviewed_at")]
    public DateTime? ReviewedAt { get; set; }

    [Column("submitted_at")]
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CampaignId))]
    public virtual Campaign? Campaign { get; set; }

    [ForeignKey(nameof(StudentUserId))]
    public virtual ApplicationUser? StudentUser { get; set; }

    [ForeignKey(nameof(InternshipPlaceId))]
    public virtual InternshipPlace? InternshipPlace { get; set; }

    [ForeignKey(nameof(ReviewedByUserId))]
    public virtual ApplicationUser? ReviewedByUser { get; set; }

    public virtual ICollection<InternshipRegistrationReview> Reviews { get; set; }
        = new List<InternshipRegistrationReview>();
}