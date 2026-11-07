using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.ViewModels.Account;

namespace InternshipManagement.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // 1. Find user by UserName or Email
        var user = await _userManager.FindByNameAsync(model.UserName);
        if (user == null && model.UserName.Contains('@'))
        {
            user = await _userManager.FindByEmailAsync(model.UserName);
        }

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác.");
            return View(model);
        }

        // 2. Check if user is active (US-01 AC: IsActive = false cannot login)
        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.");
            return View(model);
        }

        // 3. Check if user is locked out
        if (await _userManager.IsLockedOutAsync(user))
        {
            ModelState.AddModelError(string.Empty, "Tài khoản của bạn hiện đang bị khóa tạm thời. Vui lòng thử lại sau.");
            return View(model);
        }

        // 4. Attempt password sign-in
        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true
        );

        if (result.Succeeded)
        {
            _logger.LogInformation("Người dùng {UserName} đăng nhập thành công.", user.UserName);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Role-based redirection after login
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(AppRoles.Admin))
            {
                return RedirectToAction("Index", "Users");
            }
            if (roles.Contains(AppRoles.TrainingManager) || roles.Contains(AppRoles.FacultyManager))
            {
                return RedirectToAction("Index", "Campaigns");
            }

            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Tài khoản {UserName} bị khóa do đăng nhập sai nhiều lần.", user.UserName);
            ModelState.AddModelError(string.Empty, "Tài khoản đã bị tạm khóa do nhập sai mật khẩu quá số lần quy định.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác.");
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("Người dùng đã đăng xuất.");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
    [Authorize, HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());
    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model) {
        if (!ModelState.IsValid) return View(model);
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded) { foreach (var e in result.Errors) ModelState.AddModelError("",e.Description); return View(model); }
        await _signInManager.RefreshSignInAsync(user);
        TempData["SuccessMessage"]="Đã đổi mật khẩu."; return RedirectToAction("Index","Home");
    }
    [AllowAnonymous,HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());
    [AllowAnonymous,HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, [FromServices] Services.PasswordRecoveryMailer mailer) {
        if (!ModelState.IsValid) return View(model);
        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user != null && user.IsActive && user.EmailConfirmed) {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            try { await mailer.SendAsync(user.Email!, token); }
            catch (Exception e) { _logger.LogError(e,"Không gửi được email khôi phục"); ModelState.AddModelError("","Chưa gửi được yêu cầu. Vui lòng thử lại sau.");return View(model); }
        }
        return View("RecoverySent");
    }
    [AllowAnonymous,HttpGet]
    public IActionResult ResetPassword(string email,string token) => View(new ResetPasswordViewModel {Email=email,Token=token});
    [AllowAnonymous,HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model) {
        if (!ModelState.IsValid) return View(model);
        var user=await _userManager.FindByEmailAsync(model.Email);
        var result=user == null || !user.IsActive ? IdentityResult.Failed(new IdentityError {Description="Liên kết không hợp lệ hoặc đã hết hạn."}) : await _userManager.ResetPasswordAsync(user,model.Token,model.NewPassword);
        if (!result.Succeeded) { ModelState.AddModelError("","Liên kết không hợp lệ, đã dùng hoặc đã hết hạn.");return View(model); }
        await _userManager.ResetAccessFailedCountAsync(user!);
        await _userManager.SetLockoutEndDateAsync(user!,null);
        TempData["SuccessMessage"]="Đặt lại mật khẩu thành công. Vui lòng đăng nhập.";
        return RedirectToAction(nameof(Login));
    }

}
