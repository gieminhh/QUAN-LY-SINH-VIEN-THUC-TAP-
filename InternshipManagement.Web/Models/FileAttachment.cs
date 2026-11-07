using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

public static class FileCategories
{
    public const string StudentCv = "STUDENT_CV";
    public const string TopicDocument = "TOPIC_DOCUMENT";
}

[Table("file_attachments")]
public class FileAttachment
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("student_profile_id")]
    public ulong? StudentProfileId { get; set; }

    [Column("topic_id")]
    public ulong? TopicId { get; set; }

    [Required]
    [MaxLength(40)]
    [Column("file_category")]
    public string FileCategory { get; set; } = FileCategories.StudentCv;

    [Required]
    [MaxLength(255)]
    [Column("original_file_name")]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [Column("storage_key")]
    public string StorageKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    [Column("content_type")]
    public string ContentType { get; set; } = "application/octet-stream";

    [Column("size_bytes")]
    public ulong SizeBytes { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("uploaded_by_user_id")]
    public string UploadedByUserId { get; set; } = string.Empty;

    [Column("uploaded_at")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigations
    [ForeignKey(nameof(StudentProfileId))]
    public virtual StudentProfile? StudentProfile { get; set; }

    [ForeignKey(nameof(TopicId))]
    public virtual Topic? Topic { get; set; }

    [ForeignKey(nameof(UploadedByUserId))]
    public virtual ApplicationUser? UploadedByUser { get; set; }
}
