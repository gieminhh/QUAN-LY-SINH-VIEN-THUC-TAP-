using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

[Table("student_classes")]
public class StudentClass
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("major_id")]
    public ulong MajorId { get; set; }

    [Required]
    [MaxLength(40)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(180)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(30)]
    [Column("cohort")]
    public string? Cohort { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigations
    [ForeignKey(nameof(MajorId))]
    public virtual Major? Major { get; set; }

    public virtual ICollection<StudentProfile> StudentProfiles { get; set; } = new List<StudentProfile>();
}
