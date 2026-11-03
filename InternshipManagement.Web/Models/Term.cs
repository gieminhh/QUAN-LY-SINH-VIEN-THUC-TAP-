using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

[Table("terms")]
public class Term
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("academic_year_id")]
    public ulong AcademicYearId { get; set; }

    [Column("term_no")]
    public byte TermNo { get; set; }

    [Column("start_date", TypeName = "date")]
    public DateTime StartDate { get; set; }

    [Column("end_date", TypeName = "date")]
    public DateTime EndDate { get; set; }

    // Navigation
    [ForeignKey(nameof(AcademicYearId))]
    public virtual AcademicYear? AcademicYear { get; set; }

    public virtual ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
}
