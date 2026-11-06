
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

// Các trạng thái của đề tài
public static class TopicStatuses
{
    public const string Draft = "DRAFT";
    public const string Pending = "PENDING";
    public const string Approved = "APPROVED";
    public const string RevisionRequested = "REVISION_REQUESTED";
    public const string Rejected = "REJECTED";
}

[Table("topics")]
public class Topic
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Required]
    [MaxLength(250)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(5000)]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [MaxLength(3000)]
    [Column("objectives")]
    public string? Objectives { get; set; }

    [MaxLength(3000)]
    [Column("requirements")]
    public string? Requirements { get; set; }

    [Column("campaign_id")]
    public ulong CampaignId { get; set; }

    [Column("major_id")]
    public ulong MajorId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("created_by_user_id")]
    public string CreatedByUserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    [Column("status")]
    public string Status { get; set; } = TopicStatuses.Draft;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(CampaignId))]
    public virtual Campaign? Campaign { get; set; }

    [ForeignKey(nameof(MajorId))]
    public virtual Major? Major { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public virtual ApplicationUser? CreatedByUser { get; set; }

    public virtual ICollection<TopicReview> Reviews { get; set; }
        = new List<TopicReview>();
}
