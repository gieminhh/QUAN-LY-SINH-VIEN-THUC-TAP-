using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

[Table("majors")]
public class Major
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("faculty_id")]
    public ulong FacultyId { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation
    [ForeignKey(nameof(FacultyId))]
    public virtual Faculty? Faculty { get; set; }

    public virtual ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
    public virtual ICollection<UserRoleScope> RoleScopes { get; set; } = new List<UserRoleScope>();
}
