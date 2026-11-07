using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

public static class TopicActionTypes
{
    public const string Submit = "SUBMIT";
    public const string Resubmit = "RESUBMIT";
    public const string RequestRevision = "REQUEST_REVISION";
    public const string Approve = "APPROVE";
    public const string Reject = "REJECT";

    public static string GetDisplayName(string action) => action switch
    {
        Submit => "Gửi xét duyệt",
        Resubmit => "Gửi lại xét duyệt",
        RequestRevision => "Yêu cầu chỉnh sửa",
        Approve => "Phê duyệt",
        Reject => "Từ chối",
        _ => action
    };

    public static string GetBadgeClass(string action) => action switch
    {
        Submit => "badge bg-primary",
        Resubmit => "badge bg-info text-dark",
        RequestRevision => "badge bg-warning text-dark",
        Approve => "badge bg-success",
        Reject => "badge bg-danger",
        _ => "badge bg-secondary"
    };
}

[Table("topic_approval_history")]
public class TopicApprovalHistory
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("topic_id")]
    public ulong TopicId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("actor_user_id")]
    public string ActorUserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    [Column("action_type")]
    public string ActionType { get; set; } = TopicActionTypes.Submit;

    [MaxLength(30)]
    [Column("from_status")]
    public string? FromStatus { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("to_status")]
    public string ToStatus { get; set; } = TopicStatuses.Pending;

    [Column("comment_text")]
    public string? CommentText { get; set; }

    [Column("action_at")]
    public DateTime ActionAt { get; set; } = DateTime.UtcNow;

    // Navigations
    [ForeignKey(nameof(TopicId))]
    public virtual Topic? Topic { get; set; }

    [ForeignKey(nameof(ActorUserId))]
    public virtual ApplicationUser? ActorUser { get; set; }
}
