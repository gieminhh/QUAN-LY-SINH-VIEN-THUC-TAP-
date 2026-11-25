using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

public static class InternshipRegistrationStatuses
{
    public const string Pending = "PENDING";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
}

[Table("internship_places")]
public class InternshipPlace
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Required]
    [MaxLength(250)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("address")]
    public string? Address { get; set; }

    [MaxLength(180)]
    [Column("contact_person")]
    public string? ContactPerson { get; set; }

    [MaxLength(50)]
    [Column("contact_phone")]
    public string? ContactPhone { get; set; }

    [MaxLength(180)]
    [Column("contact_email")]
    public string? ContactEmail { get; set; }

    [MaxLength(200)]
    [Column("default_position")]
    public string? DefaultPosition { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [MaxLength(255)]
    [Column("created_by_user_id")]
    public string CreatedByUserId { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CreatedByUserId))]
    public virtual ApplicationUser? CreatedByUser { get; set; }

    public virtual ICollection<InternshipRegistration> Registrations { get; set; }
        = new List<InternshipRegistration>();
}