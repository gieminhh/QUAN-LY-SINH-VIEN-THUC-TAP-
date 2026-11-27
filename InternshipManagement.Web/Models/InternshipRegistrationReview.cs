using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

public static class InternshipReviewActions
{
    public const string Submitted = "SUBMITTED";
    public const string Resubmitted = "RESUBMITTED";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
}

[Table("internship_registration_reviews")]
public class InternshipRegistrationReview
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("registration_id")]
    public ulong RegistrationId { get; set; }

    [Column("attempt_no")]
    public int AttemptNo { get; set; } = 1;

    [Required]
    [MaxLength(30)]
    [Column("action")]
    public string Action { get; set; } = InternshipReviewActions.Submitted;

    [Column("comment", TypeName = "longtext")]
    public string? Comment { get; set; }

    [MaxLength(250)]
    [Column("place_name_snapshot")]
    public string? PlaceNameSnapshot { get; set; }

    [MaxLength(200)]
    [Column("position_snapshot")]
    public string? PositionSnapshot { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("actor_user_id")]
    public string ActorUserId { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(RegistrationId))]
    public virtual InternshipRegistration? Registration { get; set; }

    [ForeignKey(nameof(ActorUserId))]
    public virtual ApplicationUser? ActorUser { get; set; }
}