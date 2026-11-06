using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels.Users;

namespace InternshipManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly IUserScopeService _scopeService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        IUserScopeService scopeService,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _scopeService = scopeService;
        _logger = logger;
    }

    // 1. Danh sách tài khoản (US-02)
    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? roleFilter, string? statusFilter, int page = 1)
    {
        int pageSize = 10;
        var currentUserId = _userManager.GetUserId(User);

        var query = _userManager.Users.AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(u =>
                u.UserName!.Contains(s) ||
                u.Email!.Contains(s) ||
                u.FullName.Contains(s) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(s)));
        }

        // Status Filter
        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            if (statusFilter == "active")
                query = query.Where(u => u.IsActive);
            else if (statusFilter == "inactive")
                query = query.Where(u => !u.IsActive);
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userItems = new List<UserItemViewModel>();
        foreach (var u in users)
        {
            var userRoles = await _userManager.GetRolesAsync(u);

            // Role filter check in memory if specified
            if (!string.IsNullOrWhiteSpace(roleFilter) && !userRoles.Contains(roleFilter))
            {
                continue;
            }

            var isLocked = await _userManager.IsLockedOutAsync(u);

            userItems.Add(new UserItemViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                Roles = userRoles.ToList(),
                IsActive = u.IsActive,
                IsLockedOut = isLocked,
                CreatedAt = u.CreatedAt,
                CanModify = u.Id != currentUserId // Can't lock or delete self directly
            });
        }

        var viewModel = new UserListViewModel
        {
            Users = userItems,
            Search = search,
            RoleFilter = roleFilter,
            StatusFilter = statusFilter,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync()
        };

        return View(viewModel);
    }

    // 2. Thêm tài khoản mới (US-02)
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new UserCreateViewModel
        {
            AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
            return View(model);
        }

        // Check duplicate UserName
        if (await _userManager.FindByNameAsync(model.UserName) != null)
        {
            ModelState.AddModelError(nameof(model.UserName), "Tên đăng nhập này đã được sử dụng.");
            model.AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
            return View(model);
        }

        // Check duplicate Email
        if (await _userManager.FindByEmailAsync(model.Email) != null)
        {
            ModelState.AddModelError(nameof(model.Email), "Địa chỉ Email này đã được sử dụng.");
            model.AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.UserName,
            Email = model.Email,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            EmailConfirmed = true,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            model.AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
            return View(model);
        }

        // Assign Roles
        if (model.SelectedRoles != null && model.SelectedRoles.Any())
        {
            await _userManager.AddToRolesAsync(user, model.SelectedRoles);

            // If user has TrainingManager or FacultyManager or Admin, add default global scope if admin
            var currentUserId = _userManager.GetUserId(User)!;
            foreach (var rName in model.SelectedRoles)
            {
                var role = await _roleManager.FindByNameAsync(rName);
                if (role != null)
                {
                    // By default grant global scope to Admin or TrainingManager
                    if (rName == AppRoles.Admin || rName == AppRoles.TrainingManager)
                    {
                        await _scopeService.AssignScopeAsync(user.Id, role.Id, ScopeTypes.Global, null, null, null, currentUserId);
                    }
                }
            }
        }

        TempData["SuccessMessage"] = $"Tạo tài khoản '{user.UserName}' thành công!";
        return RedirectToAction(nameof(Index));
    }

    // 3. Chỉnh sửa thông tin tài khoản (US-02)
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);
        var userRoles = await _userManager.GetRolesAsync(user);

        var model = new UserEditViewModel
        {
            Id = user.Id,
            UserName = user.UserName!,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            SelectedRoles = userRoles.ToList(),
            AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync(),
            IsSelf = user.Id == currentUserId
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);
        model.IsSelf = user.Id == currentUserId;
        model.AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Check email duplicate if changed
        if (model.Email != user.Email)
        {
            var existingByEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingByEmail != null && existingByEmail.Id != user.Id)
            {
                ModelState.AddModelError(nameof(model.Email), "Email này đã được tài khoản khác sử dụng.");
                return View(model);
            }
            user.Email = model.Email;
        }

        // AC: Prevent self-lockout or removing last admin
        var currentRoles = await _userManager.GetRolesAsync(user);
        var hadAdmin = currentRoles.Contains(AppRoles.Admin);
        var willHaveAdmin = model.SelectedRoles.Contains(AppRoles.Admin);

        if (hadAdmin && !willHaveAdmin)
        {
            var adminCount = (await _userManager.GetUsersInRoleAsync(AppRoles.Admin)).Count;
            if (adminCount <= 1)
            {
                ModelState.AddModelError(string.Empty, "Không thể gỡ bỏ vai trò Admin của quản trị viên duy nhất còn lại trong hệ thống.");
                return View(model);
            }
        }

        if (model.IsSelf && !model.IsActive)
        {
            ModelState.AddModelError(nameof(model.IsActive), "Bạn không thể tự vô hiệu hóa tài khoản quản trị đang sử dụng.");
            return View(model);
        }

        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;
        user.IsActive = model.IsActive;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        // Update Roles
        var rolesToAdd = model.SelectedRoles.Except(currentRoles).ToList();
        var rolesToRemove = currentRoles.Except(model.SelectedRoles).ToList();

        if (rolesToAdd.Any())
        {
            await _userManager.AddToRolesAsync(user, rolesToAdd);
        }
        if (rolesToRemove.Any())
        {
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
        }

        // Optional reset password by admin
        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (!resetResult.Succeeded)
            {
                foreach (var err in resetResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, $"Lỗi đổi mật khẩu: {err.Description}");
                }
                return View(model);
            }
        }

        TempData["SuccessMessage"] = $"Cập nhật tài khoản '{user.UserName}' thành công!";
        return RedirectToAction(nameof(Index));
    }

    // 4. Khóa / Mở khóa tài khoản (US-02)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);
        if (user.Id == currentUserId)
        {
            TempData["ErrorMessage"] = "Bạn không thể tự khóa tài khoản của chính mình.";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        var statusText = user.IsActive ? "kích hoạt" : "vô hiệu hóa";
        TempData["SuccessMessage"] = $"Đã {statusText} tài khoản '{user.UserName}' thành công.";
        return RedirectToAction(nameof(Index));
    }

    // 5. Quản lý phân quyền theo phạm vi (US-03)
    [HttpGet]
    public async Task<IActionResult> ManageScopes(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var userRoles = (await _userManager.GetRolesAsync(user)).ToList();
        var existingScopes = await _scopeService.GetUserScopesAsync(user.Id);

        var scopeDtos = existingScopes.Select(s => new ScopeItemDto
        {
            Id = s.Id,
            RoleName = s.Role?.Name ?? "N/A",
            ScopeType = s.ScopeType,
            TargetDescription = s.ScopeType switch
            {
                ScopeTypes.Global => "Toàn hệ thống (GLOBAL)",
                ScopeTypes.Faculty => $"Khoa: {s.Faculty?.Name ?? s.FacultyId?.ToString()}",
                ScopeTypes.Major => $"Ngành: {s.Major?.Name ?? s.MajorId?.ToString()}",
                ScopeTypes.Campaign => $"Đợt: {s.Campaign?.Name ?? s.CampaignId?.ToString()}",
                _ => s.ScopeType
            },
            GrantedAt = s.GrantedAt,
            GrantedByName = s.GrantedByUser?.FullName ?? s.GrantedByUserId
        }).ToList();

        // Populate dropdowns
        var roles = await _context.Roles
            .Where(r => userRoles.Contains(r.Name!))
            .Select(r => new SelectListItem { Value = r.Id, Text = r.Name })
            .ToListAsync();

        var faculties = await _context.Faculties
            .Where(f => f.IsActive)
            .Select(f => new SelectListItem { Value = f.Id.ToString(), Text = $"{f.Code} - {f.Name}" })
            .ToListAsync();

        var majors = await _context.Majors
            .Where(m => m.IsActive)
            .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = $"{m.Code} - {m.Name}" })
            .ToListAsync();

        var campaigns = await _context.Campaigns
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = $"{c.Code} - {c.Name}" })
            .ToListAsync();

        var viewModel = new AssignRoleScopeViewModel
        {
            UserId = user.Id,
            FullName = user.FullName,
            UserName = user.UserName!,
            UserRoles = userRoles,
            ExistingScopes = scopeDtos,
            RoleOptions = roles,
            FacultyOptions = faculties,
            MajorOptions = majors,
            CampaignOptions = campaigns
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddScope(AssignRoleScopeViewModel model)
    {
        var currentUserId = _userManager.GetUserId(User)!;

        var success = await _scopeService.AssignScopeAsync(
            model.UserId,
            model.SelectedRoleId,
            model.SelectedScopeType,
            model.SelectedFacultyId,
            model.SelectedMajorId,
            model.SelectedCampaignId,
            currentUserId);

        if (!success)
        {
            TempData["ErrorMessage"] = "Không thể gán quyền: Thông tin phạm vi không hợp lệ hoặc người dùng chưa có vai trò này.";
        }
        else
        {
            TempData["SuccessMessage"] = "Đã gán phạm vi quyền thành công!";
        }

        return RedirectToAction(nameof(ManageScopes), new { id = model.UserId });
    }

        
// Xác nhận nhập tài khoản từ Excel
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ConfirmImport(ImportUsersViewModel model)
{
    if (model.Rows == null || !model.Rows.Any())
    {
        TempData["ErrorMessage"] = "Không có dữ liệu để nhập.";
        return RedirectToAction(nameof(ImportExcel));
    }

    if (model.Rows.Count > 200)
    {
        TempData["ErrorMessage"] = "Chỉ được nhập tối đa 200 tài khoản mỗi lần.";
        return RedirectToAction(nameof(ImportExcel));
    }

    var resultModel = new ImportUsersViewModel();

    var userNames = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase);

    var emails = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase);

    foreach (var row in model.Rows)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(row.UserName))
            errors.Add("Thiếu tên đăng nhập.");

        if (string.IsNullOrWhiteSpace(row.Email) ||
            !new EmailAddressAttribute().IsValid(row.Email))
            errors.Add("Email không hợp lệ.");

        if (string.IsNullOrWhiteSpace(row.FullName))
            errors.Add("Thiếu họ tên.");

        if (string.IsNullOrWhiteSpace(row.Role) ||
            !await _roleManager.RoleExistsAsync(row.Role))
            errors.Add("Vai trò không hợp lệ.");

        if (!string.IsNullOrWhiteSpace(row.UserName))
        {
            if (!userNames.Add(row.UserName))
                errors.Add("Tên đăng nhập bị trùng trong file.");

            if (await _userManager.FindByNameAsync(row.UserName) != null)
                errors.Add("Tên đăng nhập đã tồn tại.");
        }

        if (!string.IsNullOrWhiteSpace(row.Email))
        {
            if (!emails.Add(row.Email))
                errors.Add("Email bị trùng trong file.");

            if (await _userManager.FindByEmailAsync(row.Email) != null)
                errors.Add("Email đã tồn tại.");
        }

        if (errors.Any())
        {
            row.IsValid = false;
            row.ErrorMessage = string.Join(" ", errors);
            resultModel.Rows.Add(row);
            continue;
        }

        var password = GenerateTemporaryPassword();

        var user = new ApplicationUser
        {
            UserName = row.UserName.Trim(),
            Email = row.Email.Trim(),
            FullName = row.FullName.Trim(),
            PhoneNumber = row.PhoneNumber?.Trim(),
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
        {
            row.IsValid = false;
            row.ErrorMessage = string.Join(" ",
                createResult.Errors.Select(e => e.Description));

            resultModel.Rows.Add(row);
            continue;
        }

        var roleResult = await _userManager.AddToRoleAsync(user, row.Role);

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            row.IsValid = false;
            row.ErrorMessage = string.Join(" ",
                roleResult.Errors.Select(e => e.Description));

            resultModel.Rows.Add(row);
            continue;
        }

        if (row.Role == AppRoles.Admin ||
            row.Role == AppRoles.TrainingManager)
        {
            var role = await _roleManager.FindByNameAsync(row.Role);
            var currentUserId = _userManager.GetUserId(User)!;

            var scopeSuccess = role != null &&
                await _scopeService.AssignScopeAsync(
                    user.Id,
                    role.Id,
                    ScopeTypes.Global,
                    null,
                    null,
                    null,
                    currentUserId);

            if (!scopeSuccess)
            {
                await _userManager.RemoveFromRoleAsync(user, row.Role);
                await _userManager.DeleteAsync(user);

                row.IsValid = false;
                row.ErrorMessage = "Không thể gán phạm vi quyền.";

                resultModel.Rows.Add(row);
                continue;
            }
        }

        row.IsValid = true;
        row.ErrorMessage = "Tạo tài khoản thành công.";
        row.TemporaryPassword = password;

        resultModel.Rows.Add(row);
    }

    return View("ImportResult", resultModel);
}

// Sinh mật khẩu ngẫu nhiên
private static string GenerateTemporaryPassword()
{
    const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    const string lower = "abcdefghijkmnopqrstuvwxyz";
    const string digits = "23456789";
    const string special = "!@#$%*";

    var all = upper + lower + digits + special;

    var chars = new List<char>
    {
        upper[RandomNumberGenerator.GetInt32(upper.Length)],
        lower[RandomNumberGenerator.GetInt32(lower.Length)],
        digits[RandomNumberGenerator.GetInt32(digits.Length)],
        special[RandomNumberGenerator.GetInt32(special.Length)]
    };

    while (chars.Count < 16)
    {
        chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);
    }

    // Trộn ngẫu nhiên các ký tự
    for (int i = chars.Count - 1; i > 0; i--)
    {
        int j = RandomNumberGenerator.GetInt32(i + 1);
        (chars[i], chars[j]) = (chars[j], chars[i]);
    }

    return new string(chars.ToArray());
}

    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveScope(ulong scopeId, string userId)
    {
        var success = await _scopeService.RemoveScopeAsync(scopeId);
        if (success)
        {
            TempData["SuccessMessage"] = "Đã gỡ bỏ phạm vi quyền thành công.";
        }
        else
        {
            TempData["ErrorMessage"] = "Không tìm thấy quyền cần gỡ bỏ.";
        }

        return RedirectToAction(nameof(ManageScopes), new { id = userId });
    }
}
