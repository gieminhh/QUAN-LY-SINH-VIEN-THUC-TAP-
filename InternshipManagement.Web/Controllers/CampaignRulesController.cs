using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels;
namespace InternshipManagement.Web.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.TrainingManager},{AppRoles.FacultyManager}")]
public class CampaignRulesController(ApplicationDbContext db, IUserScopeService scopes, CampaignRulesService rules) : Controller
{
    string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpGet]
    public async Task<IActionResult> Index(ulong? campaignId, string? eligibility)
    {
        var query = await scopes.FilterAccessibleCampaignsAsync(Actor, db.Campaigns); var campaigns = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
        if (campaigns.Count == 0) { TempData["ErrorMessage"] = "Chưa có đợt trong phạm vi."; return RedirectToAction("Index", "Home"); }
        var campaign = campaigns.FirstOrDefault(c => c.Id == (campaignId ?? campaigns[0].Id)); if (campaign == null) return Forbid();
        var m = new CampaignRulesViewModel { Campaign = campaign, Campaigns = campaigns, Windows = await db.CampaignWindows.Where(x => x.CampaignId == campaign.Id).ToListAsync(), Policy = await db.CampaignEligibilityPolicies.FirstOrDefaultAsync(x => x.CampaignId == campaign.Id) ?? new(), Exceptions = await db.CampaignExceptions.Where(x => x.CampaignId == campaign.Id).ToListAsync() };
        var facultyId = await db.Majors.Where(x => x.Id == campaign.MajorId).Select(x => x.FacultyId).FirstAsync();
        m.ExceptionUsers = await db.Users.Where(u => u.IsActive && (u.Id == Actor || db.StudentProfiles.Any(p => p.UserId == u.Id && p.MajorId == campaign.MajorId) || db.LecturerProfiles.Any(p => p.UserId == u.Id && p.FacultyId == facultyId))).OrderBy(u => u.FullName).Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(u.UserName + " — " + u.FullName, u.Id)).ToListAsync();
        var students = await db.StudentProfiles.Include(s => s.User).Where(s => s.MajorId == campaign.MajorId).ToListAsync();
        foreach (var student in students) { var error = await rules.Eligibility(campaign.Id, student); if (eligibility == "eligible" && error != null || eligibility == "ineligible" && error == null) continue; m.Students.Add(new(student.Id, student.StudentCode, student.User!.FullName, error)); }
        return View(m);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveWindow(ulong campaignId, string stage, DateTime opensAt, DateTime closesAt)
    {
        if (!await scopes.CanManageCampaignAsync(Actor, campaignId)) return Forbid();
        if (!ModelState.IsValid) return BadRequest("Dữ liệu ngày, số hoặc điều kiện không hợp lệ.");
        var campaign = await db.Campaigns.FindAsync(campaignId); var other = await db.CampaignWindows.Where(x => x.CampaignId == campaignId && x.Stage != stage).ToListAsync();
        var order = Array.IndexOf(CampaignRulesService.Stages, stage);
        if (campaign == null || order < 0 || opensAt == default || closesAt < opensAt || opensAt.Date < campaign.StartDate.Date || closesAt.Date > campaign.EndDate.Date || other.Any(x => Array.IndexOf(CampaignRulesService.Stages, x.Stage) < order ? x.ClosesAt >= opensAt : x.OpensAt <= closesAt)) TempData["ErrorMessage"] = "Mốc phải nằm trong đợt, ngày mở trước ngày đóng và các giai đoạn theo đúng thứ tự, không chồng lấn.";
        else { var w = await db.CampaignWindows.FirstOrDefaultAsync(x => x.CampaignId == campaignId && x.Stage == stage); if (w == null) { w = new() { CampaignId = campaignId, Stage = stage }; db.CampaignWindows.Add(w); } w.OpensAt = opensAt; w.ClosesAt = closesAt; await db.SaveChangesAsync(); TempData["SuccessMessage"] = "Đã lưu lịch."; }
        return RedirectToAction(nameof(Index), new { campaignId });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePolicy(ulong campaignId, decimal? minimumGpa4, decimal? minimumGpa10, uint? minimumCredits, string? prerequisiteNote)
    {
        if (!await scopes.CanManageCampaignAsync(Actor, campaignId)) return Forbid();
        if (!ModelState.IsValid) return BadRequest("Dữ liệu ngày, số hoặc điều kiện không hợp lệ.");
        if (minimumGpa4 is < 0 or > 4 || minimumGpa10 is < 0 or > 10 || minimumCredits > 500 || prerequisiteNote?.Length > 1000) { TempData["ErrorMessage"] = "Điều kiện không hợp lệ."; return RedirectToAction(nameof(Index), new { campaignId }); }
        var p = await db.CampaignEligibilityPolicies.FirstOrDefaultAsync(x => x.CampaignId == campaignId); if (p == null) { p = new() { CampaignId = campaignId }; db.CampaignEligibilityPolicies.Add(p); }
        p.MinimumGpa4 = minimumGpa4; p.MinimumGpa10 = minimumGpa10; p.MinimumCredits = minimumCredits; p.PrerequisiteNote = prerequisiteNote?.Trim(); await db.SaveChangesAsync(); TempData["SuccessMessage"] = "Đã lưu điều kiện. Ô để trống không được dùng để xét."; return RedirectToAction(nameof(Index), new { campaignId });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEvidence(ulong campaignId, ulong studentId, uint credits, bool prerequisitesMet, string sourceNote)
    {
        if (!await scopes.CanManageCampaignAsync(Actor, campaignId)) return Forbid();
        if (!ModelState.IsValid) return BadRequest("Dữ liệu ngày, số hoặc điều kiện không hợp lệ.");
        var c = await db.Campaigns.FindAsync(campaignId); var student = await db.StudentProfiles.FindAsync(studentId);
        if (c == null || student?.MajorId != c.MajorId || credits > 500 || string.IsNullOrWhiteSpace(sourceNote) || sourceNote.Length > 1000) return BadRequest("Minh chứng không hợp lệ.");
        var e = await db.StudentEligibilityEvidence.FirstOrDefaultAsync(x => x.CampaignId == campaignId && x.StudentProfileId == studentId); if (e == null) { e = new() { CampaignId = campaignId, StudentProfileId = studentId }; db.StudentEligibilityEvidence.Add(e); }
        e.Credits = credits; e.PrerequisitesMet = prerequisitesMet; e.SourceNote = sourceNote.Trim(); e.RecordedBy = Actor; e.RecordedAt = DateTime.UtcNow; await db.SaveChangesAsync(); return RedirectToAction(nameof(Index), new { campaignId });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GrantException(ulong campaignId, string userId, string stage, DateTime expiresAt, string reason)
    {
        if (!await scopes.CanManageCampaignAsync(Actor, campaignId)) return Forbid();
        if (!ModelState.IsValid) return BadRequest("Dữ liệu ngày, số hoặc điều kiện không hợp lệ.");
        var user = await db.Users.FindAsync(userId);
        var campaign = await db.Campaigns.FindAsync(campaignId);
        var facultyId = await db.Majors.Where(x => x.Id == campaign!.MajorId).Select(x => x.FacultyId).FirstAsync();
        if (userId != Actor && !await db.StudentProfiles.AnyAsync(p => p.UserId == userId && p.MajorId == campaign!.MajorId) && !await db.LecturerProfiles.AnyAsync(p => p.UserId == userId && p.FacultyId == facultyId)) return BadRequest("Tài khoản ngoài khoa/ngành của đợt.");
        if (user == null || !user.IsActive || !CampaignRulesService.Stages.Contains(stage) || expiresAt <= CampaignRulesService.Now || string.IsNullOrWhiteSpace(reason) || reason.Length > 1000) return BadRequest("Ngoại lệ cần người dùng, giai đoạn, lý do và thời hạn hợp lệ.");
        db.CampaignExceptions.Add(new() { CampaignId = campaignId, UserId = userId, Stage = stage, ExpiresAt = expiresAt, Reason = reason.Trim(), ApprovedBy = Actor }); await db.SaveChangesAsync(); return RedirectToAction(nameof(Index), new { campaignId });
    }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> RevokeException(ulong id) { var e = await db.CampaignExceptions.FindAsync(id); if (e == null) return NotFound(); if (!await scopes.CanManageCampaignAsync(Actor, e.CampaignId)) return Forbid(); db.CampaignExceptions.Remove(e); await db.SaveChangesAsync(); return RedirectToAction(nameof(Index), new { campaignId = e.CampaignId }); }
}
