using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

[Table("topic_student_assignments")]
public class TopicStudentAssignment
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("topic_id")]
    public ulong TopicId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("student_user_id")]
    public string StudentUserId { get; set; } = string.Empty;

    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(TopicId))]
    public virtual Topic? Topic { get; set; }

    [ForeignKey(nameof(StudentUserId))]
    public virtual ApplicationUser? StudentUser { get; set; }
}