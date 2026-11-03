using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

[Table("faculties")]
public class Faculty
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

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
    public virtual ICollection<Major> Majors { get; set; } = new List<Major>();
    public virtual ICollection<UserRoleScope> RoleScopes { get; set; } = new List<UserRoleScope>();
}
