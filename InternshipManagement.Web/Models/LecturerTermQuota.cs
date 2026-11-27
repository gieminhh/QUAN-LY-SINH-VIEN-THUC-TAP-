
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

[Table("lecturer_term_quotas")]
public class LecturerTermQuota
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("term_id")]
    public ulong TermId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("lecturer_user_id")]
    public string LecturerUserId { get; set; } = string.Empty;

    [Range(0, 1000)]
    [Column("capacity")]
    public int Capacity { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(TermId))]
    public virtual Term? Term { get; set; }

    [ForeignKey(nameof(LecturerUserId))]
    public virtual ApplicationUser? Lecturer { get; set; }
}
