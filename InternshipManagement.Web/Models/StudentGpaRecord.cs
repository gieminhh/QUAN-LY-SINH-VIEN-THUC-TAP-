using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

public static class GpaSourceTypes
{
    public const string Api = "API";
    public const string Excel = "EXCEL";
    public const string Manual = "MANUAL";
}

[Table("student_gpa_records")]
public class StudentGpaRecord
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("student_profile_id")]
    public ulong StudentProfileId { get; set; }

    [Column("term_id")]
    public ulong TermId { get; set; }

    [Column("gpa_4", TypeName = "decimal(4,2)")]
    public decimal? Gpa4 { get; set; }

    [Column("gpa_10", TypeName = "decimal(4,2)")]
    public decimal? Gpa10 { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("source_type")]
    public string SourceType { get; set; } = GpaSourceTypes.Manual;

    [MaxLength(255)]
    [Column("recorded_by_user_id")]
    public string? RecordedByUserId { get; set; }

    [Column("recorded_at")]
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigations
    [ForeignKey(nameof(StudentProfileId))]
    public virtual StudentProfile? StudentProfile { get; set; }

    [ForeignKey(nameof(TermId))]
    public virtual Term? Term { get; set; }

    [ForeignKey(nameof(RecordedByUserId))]
    public virtual ApplicationUser? RecordedByUser { get; set; }
}
