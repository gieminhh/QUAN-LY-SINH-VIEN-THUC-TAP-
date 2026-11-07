using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

public static class TopicStatuses
{
    public const string Draft = "DRAFT";
    public const string Pending = "PENDING";
    public const string RevisionRequired = "REVISION_REQUIRED";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";

    public static string GetDisplayName(string status) => status switch
    {
        Draft => "Bản nháp",
        Pending => "Chờ xét duyệt",
        RevisionRequired => "Yêu cầu chỉnh sửa",
        Approved => "Đã phê duyệt",
        Rejected => "Bị từ chối",
        _ => status
    };

    public static string GetBadgeClass(string status) => status switch
    {
        Draft => "badge bg-secondary",
        Pending => "badge bg-warning text-dark",
        RevisionRequired => "badge bg-info text-dark",
        Approved => "badge bg-success",
        Rejected => "badge bg-danger",
        _ => "badge bg-light text-dark"
    };
}

[Table("topics")]
public class Topic
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("campaign_id")]
    public ulong CampaignId { get; set; }

    [Column("lecturer_profile_id")]
    public ulong LecturerProfileId { get; set; }

    [Required]
    [MaxLength(60)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("objectives")]
    public string? Objectives { get; set; }

    [Column("knowledge_requirements")]
    public string? KnowledgeRequirements { get; set; }

    [Column("technologies")]
    public string? Technologies { get; set; }

    [MaxLength(180)]
    [Column("research_field")]
    public string? ResearchField { get; set; }

    [Column("max_students")]
    public uint MaxStudents { get; set; } = 1;

    [Required]
    [MaxLength(30)]
    [Column("status")]
    public string Status { get; set; } = TopicStatuses.Draft;

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigations
    [ForeignKey(nameof(CampaignId))]
    public virtual Campaign? Campaign { get; set; }

    [ForeignKey(nameof(LecturerProfileId))]
    public virtual LecturerProfile? LecturerProfile { get; set; }

    public virtual ICollection<TopicApprovalHistory> ApprovalHistories { get; set; } = new List<TopicApprovalHistory>();
    public virtual ICollection<FileAttachment> Attachments { get; set; } = new List<FileAttachment>();
}
