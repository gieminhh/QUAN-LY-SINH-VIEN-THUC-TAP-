
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

[Table("topic_assignment_histories")]
public class TopicAssignmentHistory
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("assignment_id")]
    public ulong? AssignmentId { get; set; }

    [Column("topic_id")]
    public ulong TopicId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("student_user_id")]
    public string StudentUserId { get; set; } = string.Empty;

    [Column("group_id")]
    public ulong? GroupId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("action")]
    public string Action { get; set; } = string.Empty;

    [MaxLength(1000)]
    [Column("reason")]
    public string? Reason { get; set; }

    [MaxLength(255)]
    [Column("changed_by")]
    public string ChangedBy { get; set; } = string.Empty;

    [Column("changed_at")]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
