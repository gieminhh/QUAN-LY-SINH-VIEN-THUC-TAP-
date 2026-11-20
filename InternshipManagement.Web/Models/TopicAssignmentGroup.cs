
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

[Table("topic_assignment_groups")]
public class TopicAssignmentGroup
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("topic_id")]
    public ulong TopicId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("group_name")]
    public string GroupName { get; set; } = string.Empty;

    [Column("is_locked")]
    public bool IsLocked { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(TopicId))]
    public virtual Topic? Topic { get; set; }
}
