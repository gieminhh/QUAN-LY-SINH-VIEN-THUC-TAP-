using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels;
namespace InternshipManagement.Web.Services;

public class UserImportWorkflow(UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles, ApplicationDbContext db, IUserScopeService scopes, IDataProtectionProvider protection)
{
    record Ticket(string Actor, DateTime Expires, List<ImportUserRow> Rows);
    public record Batch(UserImportViewModel Model, List<ImportUserRow> Rows, Dictionary<int, string> Passwords);
    IDataProtector Protector => protection.CreateProtector("user-import-v1");
    async Task<string?> Validate(ImportUserRow row, HashSet<string> names, HashSet<string> emails)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(row.UserName, "^[a-zA-Z0-9_.-]{3,100}$") || row.FullName.Length is < 1 or > 180 || !new EmailAddressAttribute().IsValid(row.Email) || row.Email.Length > 256) return "Tên đăng nhập, họ tên hoặc email không hợp lệ.";
        if (row.PhoneNumber?.Length > 30) return "Số điện thoại không hợp lệ.";
        if (row.InitialPassword.Length < 6 || row.InitialPassword.Length > 100) return "Mật khẩu ban đầu phải từ 6 đến 100 ký tự.";
        if (!AppRoles.AllRoles.Contains(row.Role)) return "Vai trò không hợp lệ.";
        if (!names.Add(row.UserName) || !emails.Add(row.Email)) return "Trùng username/email trong Excel.";
        if (await users.FindByNameAsync(row.UserName) != null || await users.FindByEmailAsync(row.Email) != null) return "Username/email đã tồn tại.";
        return null;
    }
    public async Task<Batch> Preview(string actor, IFormFile? file, bool legacy = false)
    {
        var m = new UserImportViewModel();
        var parsed = new List<ImportUserRow>();
        try
        {
            if (file == null || file.Length == 0 || file.Length > 2_000_000 || Path.GetExtension(file.FileName).ToLowerInvariant() != ".xlsx") throw new InvalidDataException("Chọn file .xlsx không quá 2MB.");
            using var stream = file.OpenReadStream(); var rows = XlsxUsersReader.Read(stream, legacy); parsed = rows; if (rows.Count == 0) throw new InvalidDataException("File không có dữ liệu.");
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows) { var error = await Validate(row, names, emails); m.Results.Add(new(row.Row, row.UserName, error == null, error ?? "Hợp lệ; chưa tạo tài khoản.")); }
            if (m.Results.All(x => x.Success)) m.Ticket = Protector.Protect(JsonSerializer.Serialize(new Ticket(actor, DateTime.UtcNow.AddMinutes(15), rows)));
        }
        catch (Exception e) when (e is InvalidDataException or System.Xml.XmlException or FormatException or ArgumentException or IndexOutOfRangeException) { m.Error = "Không đọc được Excel. " + e.Message; }
        return new(m, parsed, new());
    }
    public async Task<Batch?> Confirm(string actor, string ticket)
    {
        var m = new UserImportViewModel { Finished = true }; Ticket? data;
        try { data = JsonSerializer.Deserialize<Ticket>(Protector.Unprotect(ticket)); } catch { return null; }
        if (data == null || data.Actor != actor || data.Expires < DateTime.UtcNow) return null;
        var passwords = new Dictionary<int, string>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in data.Rows)
        {
            var error = await Validate(row, names, emails); if (error != null) { m.Results.Add(new(row.Row, row.UserName, false, error)); continue; }
            await using var tx = await db.Database.BeginTransactionAsync();
            var user = new ApplicationUser { UserName = row.UserName, Email = row.Email, FullName = row.FullName, PhoneNumber = row.PhoneNumber, IsActive = true, EmailConfirmed = false, CreatedAt = DateTime.UtcNow };
            var result = await users.CreateAsync(user, row.InitialPassword);
            if (result.Succeeded) result = await users.AddToRoleAsync(user, row.Role);
            if (result.Succeeded && (row.Role == AppRoles.Admin || row.Role == AppRoles.TrainingManager))
            {
                var role = await roles.FindByNameAsync(row.Role); if (role == null || !await scopes.AssignScopeAsync(user.Id, role.Id, ScopeTypes.Global, null, null, null, data.Actor)) result = IdentityResult.Failed(new IdentityError { Description = "Không cấp được phạm vi." });
            }
            if (result.Succeeded) await tx.CommitAsync(); else { await tx.RollbackAsync(); db.ChangeTracker.Clear(); }
            if (result.Succeeded) passwords[row.Row] = row.InitialPassword;
            m.Results.Add(new(row.Row, row.UserName, result.Succeeded, result.Succeeded ? "Đã tạo tài khoản." : string.Join("; ", result.Errors.Select(e => e.Description))));
        }
        return new(m, data.Rows, passwords);
    }
}
