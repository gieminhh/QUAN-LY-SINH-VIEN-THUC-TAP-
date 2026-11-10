
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

[Table("topic_attachments")]
public class TopicAttachment
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("topic_id")]
    public ulong TopicId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("original_file_name")]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("stored_file_name")]
    public string StoredFileName { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("content_type")]
    public string? ContentType { get; set; }

    [Column("file_size")]
    public long FileSize { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("uploaded_by_user_id")]
    public string UploadedByUserId { get; set; } = string.Empty;

    [Column("uploaded_at")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual Topic? Topic { get; set; }

    public virtual ApplicationUser? UploadedByUser { get; set; }
}
