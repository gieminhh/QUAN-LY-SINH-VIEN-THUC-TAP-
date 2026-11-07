using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels;
namespace InternshipManagement.Web.Controllers;

[Authorize]
public class CompaniesController(ApplicationDbContext db, IUserScopeService scopes) : Controller
{
    string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpGet, Authorize(Roles = $"{AppRoles.Admin},{AppRoles.TrainingManager},{AppRoles.FacultyManager}")]
    public async Task<IActionResult> Index()
    {
        var majors = await scopes.FilterManageableMajorsAsync(Actor, db.Majors);
        return View(new CompaniesViewModel { Companies = await db.Companies.Where(c => majors.Any(m => m.Id == c.MajorId)).OrderByDescending(c => c.Id).ToListAsync() });
    }
    [HttpGet, Authorize(Roles = AppRoles.Company)]
    public async Task<IActionResult> Register()
    {
        var companies = await db.Companies.Where(x => x.OwnerUserId == Actor).ToListAsync(); var ids = companies.Select(c => c.Id).ToList();
        return View(new CompaniesViewModel { Companies = companies, Majors = await db.Majors.Where(m => m.IsActive).ToListAsync(), Mentors = await db.CompanyMentors.Where(m => ids.Contains(m.CompanyId)).ToListAsync() });
    }
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = AppRoles.Company)]
    public async Task<IActionResult> Register(CompanyRegistration input)
    {
        input.OwnerUserId = Actor; input.Status = "PENDING"; input.ReviewedBy = null; input.ReviewedAt = null; input.ReviewReason = null; input.Id = 0;
        ModelState.Remove(nameof(input.OwnerUserId));
        if (!ModelState.IsValid || !await db.Majors.AnyAsync(x => x.Id == input.MajorId && x.IsActive)) { TempData["ErrorMessage"] = "Vui lòng nhập đủ thông tin doanh nghiệp và ngành hợp lệ."; return RedirectToAction(nameof(Register)); }
        input.RegistrationCode = input.RegistrationCode.Trim().ToUpperInvariant();
        if (await db.Companies.AnyAsync(x => x.RegistrationCode == input.RegistrationCode)) { TempData["ErrorMessage"] = "Mã đăng ký doanh nghiệp đã tồn tại. Liên hệ cán bộ để xác minh."; return RedirectToAction(nameof(Register)); }
        db.Companies.Add(input); try { await db.SaveChangesAsync(); TempData["SuccessMessage"] = "Đã gửi yêu cầu, đang chờ xét duyệt."; } catch (DbUpdateException) { TempData["ErrorMessage"] = "Mã đăng ký đã tồn tại."; }
        return RedirectToAction(nameof(Register));
    }
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = $"{AppRoles.Admin},{AppRoles.TrainingManager},{AppRoles.FacultyManager}")]
    public async Task<IActionResult> Review(ulong id, string decision, string reason)
    {
        var c = await db.Companies.FindAsync(id); if (c == null) return NotFound(); if (!await scopes.CanManageMajorAsync(Actor, c.MajorId)) return Forbid();
        if (c.Status != "PENDING" || !new[] { "APPROVED", "REJECTED" }.Contains(decision) || string.IsNullOrWhiteSpace(reason) || reason.Length > 1000) return BadRequest("Yêu cầu phải đang chờ, có quyết định và lý do hợp lệ.");
        c.Status = decision; c.ReviewReason = reason.Trim(); c.ReviewedBy = Actor; c.ReviewedAt = DateTime.UtcNow;
        if (decision == "APPROVED" && !await db.CompanyMentors.AnyAsync(m => m.CompanyId == id && m.UserId == c.OwnerUserId)) db.CompanyMentors.Add(new() { CompanyId = id, UserId = c.OwnerUserId, Name = c.ContactName });
        await db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = AppRoles.Company)]
    public async Task<IActionResult> AddMentor(ulong companyId, string email, string name)
    {
        var c = await db.Companies.FindAsync(companyId); if (c == null || c.OwnerUserId != Actor || c.Status != "APPROVED") return Forbid();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        if (user == null || !await db.UserRoles.AnyAsync(ur => ur.UserId == user.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == AppRoles.Company)) || string.IsNullOrWhiteSpace(name) || name.Length > 180) return BadRequest("Cần email tài khoản doanh nghiệp đang hoạt động và họ tên hợp lệ.");
        if (!await db.CompanyMentors.AnyAsync(m => m.CompanyId == companyId && m.UserId == user.Id)) { db.CompanyMentors.Add(new() { CompanyId = companyId, UserId = user.Id, Name = name.Trim() }); await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Register));
    }
    [HttpGet, Authorize(Roles = AppRoles.Company)]
    public async Task<IActionResult> Students()
    {
        var query = from a in db.StudentAssignments join m in db.CompanyMentors on a.CompanyMentorId equals (ulong?)m.Id join c in db.Companies on m.CompanyId equals c.Id join s in db.StudentProfiles on a.StudentProfileId equals s.Id join u in db.Users on s.UserId equals u.Id join cp in db.Campaigns on a.CampaignId equals cp.Id where m.UserId == Actor && c.Status == "APPROVED" select new AssignedStudentRow(s.Id, s.StudentCode, u.FullName, cp.Name);
        return View(await query.ToListAsync());
    }
}
