using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

[Table("student_profiles")]
public class StudentProfile
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    [Column("student_code")]
    public string StudentCode { get; set; } = string.Empty;

    [Column("major_id")]
    public ulong MajorId { get; set; }

    [Column("class_id")]
    public ulong? ClassId { get; set; }

    [Column("skills")]
    public string? Skills { get; set; }

    [Column("interests")]
    public string? Interests { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigations
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }

    [ForeignKey(nameof(MajorId))]
    public virtual Major? Major { get; set; }

    [ForeignKey(nameof(ClassId))]
    public virtual StudentClass? Class { get; set; }

    public virtual ICollection<StudentGpaRecord> GpaRecords { get; set; } = new List<StudentGpaRecord>();
    public virtual ICollection<FileAttachment> Attachments { get; set; } = new List<FileAttachment>();
}
