using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

public enum TrangThaiUngTuyen
{
    [Display(Name = "Đã nộp")]
    DaNop,
    [Display(Name = "Đang phỏng vấn")]
    DangPhongVan,
    [Display(Name = "Chấp nhận")]
    ChapNhan,
    [Display(Name = "Từ chối")]
    TuChoi
}

[Table("ho_so_ung_tuyen")]
public class HoSoUngTuyen
{
    [Key]
    public ulong Id { get; set; }

    public ulong ViTriThucTapId { get; set; }

    [Required]
    [MaxLength(255)]
    public string SinhVienId { get; set; } = string.Empty;

    public DateTime NgayNop { get; set; } = DateTime.UtcNow;

    public TrangThaiUngTuyen TrangThai { get; set; } = TrangThaiUngTuyen.DaNop;

    public string? GhiChuPhanHoi { get; set; }

    [MaxLength(500)]
    public string? FileCV { get; set; }

    public virtual InternshipPosition? ViTriThucTap { get; set; }

    public virtual ApplicationUser? SinhVien { get; set; }
}
