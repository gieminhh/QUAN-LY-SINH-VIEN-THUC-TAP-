using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

[Table("lecturer_campaign_quotas")]
public class LecturerCampaignQuota
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("lecturer_profile_id")]
    public ulong LecturerProfileId { get; set; }

    [Column("campaign_id")]
    public ulong CampaignId { get; set; }

    [Column("max_students")]
    public uint MaxStudents { get; set; } = 0;

    [MaxLength(255)]
    [Column("assigned_by_user_id")]
    public string? AssignedByUserId { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigations
    [ForeignKey(nameof(LecturerProfileId))]
    public virtual LecturerProfile? LecturerProfile { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public virtual Campaign? Campaign { get; set; }

    [ForeignKey(nameof(AssignedByUserId))]
    public virtual ApplicationUser? AssignedByUser { get; set; }
}
