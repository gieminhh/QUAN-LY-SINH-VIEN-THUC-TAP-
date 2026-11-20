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

    [Column("group_id")]
    public ulong? GroupId { get; set; }

    [ForeignKey(nameof(GroupId))]
    public virtual TopicAssignmentGroup? Group { get; set; }

    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    [Column("is_locked")]
    public bool IsLocked { get; set; } = false;

    [MaxLength(1000)]
    [Column("adjustment_reason")]
    public string? AdjustmentReason { get; set; }

    [MaxLength(255)]
    [Column("adjusted_by")]
    public string? AdjustedBy { get; set; }

    [Column("adjusted_at")]
    public DateTime? AdjustedAt { get; set; }

    [ForeignKey(nameof(TopicId))]
    public virtual Topic? Topic { get; set; }

    [ForeignKey(nameof(StudentUserId))]
    public virtual ApplicationUser? StudentUser { get; set; }
}
