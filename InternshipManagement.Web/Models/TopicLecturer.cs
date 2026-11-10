
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

[Table("topic_lecturers")]
public class TopicLecturer
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("topic_id")]
    public ulong TopicId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("lecturer_user_id")]
    public string LecturerUserId { get; set; } = string.Empty;

    [Column("is_primary")]
    public bool IsPrimary { get; set; }

    [Column("joined_at")]
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual Topic? Topic { get; set; }

    public virtual ApplicationUser? Lecturer { get; set; }
}
