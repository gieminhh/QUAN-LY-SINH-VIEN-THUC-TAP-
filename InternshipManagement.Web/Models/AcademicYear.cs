using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

[Table("academic_years")]
public class AcademicYear
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Required]
    [MaxLength(15)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [Column("start_date", TypeName = "date")]
    public DateTime StartDate { get; set; }

    [Column("end_date", TypeName = "date")]
    public DateTime EndDate { get; set; }

    // Navigation
    public virtual ICollection<Term> Terms { get; set; } = new List<Term>();
}
