using System.ComponentModel.DataAnnotations;
namespace InternshipManagement.Web.ViewModels.Account;

public class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password), Display(Name = "Mật khẩu hiện tại")] public string CurrentPassword { get; set; } = "";
    [Required, MinLength(6), DataType(DataType.Password), Display(Name = "Mật khẩu mới")] public string NewPassword { get; set; } = "";
    [Required, Compare(nameof(NewPassword)), DataType(DataType.Password), Display(Name = "Nhập lại mật khẩu mới")] public string ConfirmPassword { get; set; } = "";
}
public class ForgotPasswordViewModel { [Required, EmailAddress] public string Email { get; set; } = ""; }
public class ResetPasswordViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Token { get; set; } = "";
    [Required, MinLength(6), DataType(DataType.Password), Display(Name = "Mật khẩu mới")] public string NewPassword { get; set; } = "";
    [Required, Compare(nameof(NewPassword)), DataType(DataType.Password), Display(Name = "Nhập lại mật khẩu mới")] public string ConfirmPassword { get; set; } = "";
}
