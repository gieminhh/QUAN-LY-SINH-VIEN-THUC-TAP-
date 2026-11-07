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
public class AssignmentsController(ApplicationDbContext db, IUserScopeService scopes, StudentAssignmentService assignments) : Controller
{
    string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpGet]
    public async Task<IActionResult> Index(ulong? campaignId)
    {
        var query = await scopes.FilterAccessibleCampaignsAsync(Actor, db.Campaigns); var campaigns = await query.Include(c => c.Major).OrderByDescending(c => c.CreatedAt).ToListAsync();
        if (campaigns.Count == 0) { TempData["ErrorMessage"] = "Chưa có đợt được phân quyền."; return RedirectToAction("Index", "Home"); }
        var campaign = campaigns.FirstOrDefault(c => c.Id == (campaignId ?? campaigns[0].Id)); if (campaign == null) return Forbid();
        return View(new AssignmentViewModel { Campaign = campaign, Campaigns = campaigns, Students = await db.StudentProfiles.Include(s => s.User).Where(s => s.MajorId == campaign.MajorId).ToListAsync(), Lecturers = await db.LecturerProfiles.Include(l => l.User).Where(l => l.FacultyId == campaign.Major!.FacultyId).ToListAsync(), Mentors = await db.CompanyMentors.Where(m => db.Companies.Any(c => c.Id == m.CompanyId && c.Status == "APPROVED" && c.MajorId == campaign.MajorId)).ToListAsync(), Assignments = await db.StudentAssignments.Where(a => a.CampaignId == campaign.Id).ToListAsync() });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(ulong campaignId, ulong studentId, ulong lecturerId, ulong? mentorId, string? quotaOverrideReason)
    {
        if (!await scopes.CanManageCampaignAsync(Actor, campaignId)) return Forbid();
        var result = await assignments.AssignAsync(Actor, campaignId, studentId, lecturerId, mentorId, quotaOverrideReason);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index), new { campaignId });
    }
}
