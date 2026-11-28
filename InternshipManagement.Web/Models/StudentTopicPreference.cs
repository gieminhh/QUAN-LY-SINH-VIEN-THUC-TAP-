using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

[Table("student_topic_preferences")]
public class StudentTopicPreference
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("student_id")]
    public string StudentId { get; set; } = string.Empty;

    [Column("topic_id")]
    public ulong TopicId { get; set; }

    [Column("priority")]
    public int Priority { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(StudentId))]
    public virtual ApplicationUser? Student { get; set; }

    [ForeignKey(nameof(TopicId))]
    public virtual Topic? Topic { get; set; }
}
