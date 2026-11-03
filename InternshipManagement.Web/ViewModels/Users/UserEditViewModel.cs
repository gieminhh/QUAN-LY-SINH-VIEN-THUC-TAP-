using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Web.ViewModels.Users;

public class UserEditViewModel
{
    public string Id { get; set; } = string.Empty;

    [Display(Name = "Tên đăng nhập")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ và tên không được để trống.")]
    [StringLength(180, ErrorMessage = "Họ và tên tối đa 180 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
    [StringLength(256, ErrorMessage = "Email tối đa 256 ký tự.")]
    [Display(Name = "Địa chỉ Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Kích hoạt tài khoản")]
    public bool IsActive { get; set; }

    [Display(Name = "Đổi mật khẩu mới (để trống nếu không đổi)")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên.")]
    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }

    [Display(Name = "Vai trò người dùng")]
    public List<string> SelectedRoles { get; set; } = new();

    public List<string> AvailableRoles { get; set; } = new();

    public bool IsSelf { get; set; }
}
