
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

// Các loại đề tài
public static class TopicTypes
{
    public const string Individual = "INDIVIDUAL";
    public const string Group = "GROUP";
}

// Các trạng thái của đề tài
public static class TopicStatuses
{
    // Trạng thái quản lý đề tài
    public const string Published = "PUBLISHED";
    public const string Hidden = "HIDDEN";
    public const string Closed = "CLOSED";

    // Trạng thái xét duyệt đề tài
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

[Column("campaign_id")]
public ulong CampaignId { get; set; }
    [Required]
    [MaxLength(250)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column("description", TypeName = "longtext")]
    public string Description { get; set; } = string.Empty;

    [Column("objectives", TypeName = "longtext")]
    public string? Objectives { get; set; }

    [MaxLength(1000)]
    [Column("specialty")]
    public string? Specialty { get; set; }

    [MaxLength(1000)]
    [Column("technologies")]
    public string? Technologies { get; set; }

    [Column("requirements", TypeName = "longtext")]
    public string? Requirements { get; set; }

    [Column("major_id")]
    public ulong MajorId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("topic_type")]
    public string TopicType { get; set; } = TopicTypes.Individual;

    [Range(1, 1000)]
    [Column("max_students")]
    public int? MaxStudents { get; set; }

    [Range(1, 1000)]
    [Column("max_groups")]
    public int? MaxGroups { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = TopicStatuses.Published;
    [Required]
    [MaxLength(255)]
    [Column("created_by_user_id")]
    public string CreatedByUserId { get; set; } = string.Empty;

    [Column("source_topic_id")]
    public ulong? SourceTopicId { get; set; }

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

    public virtual Topic? SourceTopic { get; set; }

    public virtual ICollection<Topic> DerivedTopics { get; set; }
        = new List<Topic>();

    public virtual ICollection<TopicLecturer> Lecturers { get; set; }
        = new List<TopicLecturer>();

    public virtual ICollection<TopicAttachment> Attachments { get; set; }
        = new List<TopicAttachment>();

}
