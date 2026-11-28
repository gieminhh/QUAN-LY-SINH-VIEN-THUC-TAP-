using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Web.Models;

public enum TrangThaiHopTac
{
    [Display(Name = "Chưa liên hệ")]
    ChuaLienHe,
    [Display(Name = "Đang trao đổi")]
    DangTraoDoi,
    [Display(Name = "Đang hợp tác")]
    DangHopTac,
    [Display(Name = "Tạm ngừng")]
    TamNgung,
    [Display(Name = "Kết thúc")]
    KetThuc
}

public class DoanhNghiep
{
    public ulong Id { get; set; }

    [Required]
    public string Ten { get; set; } = string.Empty;

    [Required]
    public string MaSoThue { get; set; } = string.Empty;

    public string? LinhVuc { get; set; }
    public string? DiaChi { get; set; }
    public string? NguoiLienHe { get; set; }
    public string? SoDienThoai { get; set; }
    public string? Email { get; set; }
    public TrangThaiHopTac TrangThai { get; set; } = TrangThaiHopTac.ChuaLienHe;
    public string? FileHopTac { get; set; }
}
