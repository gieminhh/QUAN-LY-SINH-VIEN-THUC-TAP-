using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

public static class InternshipPositionApprovalStatuses
{
    public const string Pending = "PENDING";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";

    public static readonly IReadOnlyList<string> All = [Pending, Approved, Rejected];

    public static string GetLabel(string? status) => status?.ToUpperInvariant() switch
    {
        Pending => "Chờ khoa duyệt",
        Approved => "Đã duyệt",
        Rejected => "Từ chối / Yêu cầu sửa",
        _ => "Không xác định"
    };
}

[Table("internship_positions")]
public class InternshipPosition
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Required]
    [Column("campaign_id")]
    public ulong CampaignId { get; set; }

    [Required]
    [Column("internship_place_id")]
    public ulong InternshipPlaceId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên vị trí thực tập.")]
    [MaxLength(250)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mô tả công việc.")]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("requirements")]
    public string? Requirements { get; set; }

    [Column("benefits")]
    public string? Benefits { get; set; }

    [MaxLength(500)]
    [Column("skills")]
    public string? Skills { get; set; }

    [Range(1, 500, ErrorMessage = "Số lượng tuyển phải từ 1 đến 500.")]
    [Column("slots")]
    public int Slots { get; set; } = 1;

    [Column("deadline")]
    public DateTime? Deadline { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("approval_status")]
    public string ApprovalStatus { get; set; } = InternshipPositionApprovalStatuses.Pending;

    [MaxLength(1000)]
    [Column("review_note")]
    public string? ReviewNote { get; set; }

    [MaxLength(255)]
    [Column("reviewed_by_user_id")]
    public string? ReviewedByUserId { get; set; }

    [Column("reviewed_at")]
    public DateTime? ReviewedAt { get; set; }

    [Column("is_open")]
    public bool IsOpen { get; set; } = true;

    [Required]
    [MaxLength(255)]
    [Column("created_by_user_id")]
    public string CreatedByUserId { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CampaignId))]
    public virtual Campaign? Campaign { get; set; }

    [ForeignKey(nameof(InternshipPlaceId))]
    public virtual InternshipPlace? InternshipPlace { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public virtual ApplicationUser? CreatedByUser { get; set; }

    [ForeignKey(nameof(ReviewedByUserId))]
    public virtual ApplicationUser? ReviewedByUser { get; set; }
}