using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

[Table("lecturer_campaign_quotas")]
public class LecturerCampaignQuota
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

    [Range(0, 1000)]
    [Column("capacity")]
    public int Capacity { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CampaignId))]
    public virtual Campaign? Campaign { get; set; }

    [ForeignKey(nameof(LecturerUserId))]
    public virtual ApplicationUser? Lecturer { get; set; }
}
