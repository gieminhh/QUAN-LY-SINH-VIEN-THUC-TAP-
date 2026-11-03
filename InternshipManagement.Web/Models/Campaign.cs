using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

public static class CampaignKinds
{
    public const string Project = "PROJECT";
    public const string Internship = "INTERNSHIP";
}

public static class CampaignStatuses
{
    public const string Draft = "DRAFT";
    public const string Open = "OPEN";
    public const string Closed = "CLOSED";
    public const string Archived = "ARCHIVED";
}

[Table("campaigns")]
public class Campaign
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("term_id")]
    public ulong TermId { get; set; }

    [Column("major_id")]
    public ulong MajorId { get; set; }

    [Required]
    [Column("kind")]
    public string Kind { get; set; } = CampaignKinds.Project;

    [Required]
    [MaxLength(80)]
    [Column("training_system")]
    public string TrainingSystem { get; set; } = "CHINH_QUY";

    [Column("start_date", TypeName = "date")]
    public DateTime StartDate { get; set; }

    [Column("end_date", TypeName = "date")]
    public DateTime EndDate { get; set; }

    [Required]
    [Column("status")]
    public string Status { get; set; } = CampaignStatuses.Draft;

    [Required]
    [MaxLength(255)]
    [Column("created_by_user_id")]
    public string CreatedByUserId { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(TermId))]
    public virtual Term? Term { get; set; }

    [ForeignKey(nameof(MajorId))]
    public virtual Major? Major { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public virtual ApplicationUser? CreatedByUser { get; set; }

    public virtual ICollection<UserRoleScope> RoleScopes { get; set; } = new List<UserRoleScope>();
}
