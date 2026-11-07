using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

[Table("lecturer_profiles")]
public class LecturerProfile
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
    [Column("lecturer_code")]
    public string LecturerCode { get; set; } = string.Empty;

    [Column("faculty_id")]
    public ulong FacultyId { get; set; }

    [MaxLength(180)]
    [Column("department_name")]
    public string? DepartmentName { get; set; }

    [MaxLength(100)]
    [Column("academic_degree")]
    public string? AcademicDegree { get; set; }

    [MaxLength(100)]
    [Column("academic_title")]
    public string? AcademicTitle { get; set; }

    [Column("expertise")]
    public string? Expertise { get; set; }

    [Column("research_interests")]
    public string? ResearchInterests { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigations
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }

    [ForeignKey(nameof(FacultyId))]
    public virtual Faculty? Faculty { get; set; }

    public virtual ICollection<LecturerCampaignQuota> Quotas { get; set; } = new List<LecturerCampaignQuota>();
    public virtual ICollection<Topic> Topics { get; set; } = new List<Topic>();
}
