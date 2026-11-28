using System.Security.Claims;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace InternshipManagement.Web.Controllers;
[Authorize(Roles = AppRoles.Student)]
public sealed class ProjectGroupsController(ApplicationDbContext db, ParticipationService service) : Controller
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpGet] public async Task<IActionResult> Index(ulong? campaignId)
    {
        if (!await service.HasRole(Actor, AppRoles.Student)) return Forbid();
        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == Actor);
        var campaigns = await db.Campaigns.AsNoTracking().Where(c => c.MajorId == user.MajorId).OrderByDescending(c => c.Id).ToListAsync();
        var c = campaignId.HasValue ? campaigns.Find(c => c.Id == campaignId) : campaigns.FirstOrDefault();
        if (campaignId.HasValue && c == null) return NotFound();
        var page = new GroupPage { Campaigns = campaigns };
        if (c == null) return View(page);
        page.CampaignId = c.Id;
        page.Policy = await db.Set<ParticipationPolicy>().AsNoTracking().SingleOrDefaultAsync(p => p.CampaignId == c.Id);
        page.CanGroup = c.Kind == CampaignKinds.Project && page.Policy?.GroupsAllowed == true;
        page.LockedReason = await service.Window(c, false);
        page.Participation = await db.Set<CampaignParticipant>().AsNoTracking().SingleOrDefaultAsync(p => p.CampaignId == c.Id && p.StudentId == Actor);
        if (page.Participation?.GroupId is { } groupId) {
            page.Group = await db.Set<ProjectRegistrationGroup>().AsNoTracking().SingleAsync(g => g.Id == groupId);
            page.IsLeader = page.Group.LeaderId == Actor;
            page.Members = await db.Users.AsNoTracking().Where(u => db.Set<CampaignParticipant>().Any(p => p.GroupId == groupId && p.StudentId == u.Id)).OrderBy(u => u.FullName).ToListAsync();
            if (page.IsLeader) {
                page.Invitees = await db.Users.AsNoTracking().Where(u => u.IsActive && u.MajorId == c.MajorId &&
                    db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == AppRoles.Student)) &&
                    !db.Set<CampaignParticipant>().Any(p => p.CampaignId == c.Id && p.StudentId == u.Id)).OrderBy(u => u.FullName).ToListAsync();
                page.SentInvitations = await db.Set<ProjectGroupInvitation>().AsNoTracking().Where(i => i.GroupId == groupId).ToListAsync();
            }
        }
        page.Invitations = await db.Set<ProjectRegistrationGroup>().AsNoTracking().Where(g => g.CampaignId == c.Id &&
            db.Set<ProjectGroupInvitation>().Any(i => i.GroupId == g.Id && i.StudentId == Actor && i.Status == "PENDING")).ToListAsync();
        return View(page);
    }
    private IActionResult Result(string? error, ulong campaignId) {
        TempData[error == null ? "SuccessMessage" : "ErrorMessage"] = error ?? "Đã cập nhật đăng ký.";
        return RedirectToAction(nameof(Index), new { campaignId });
    }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Individual(ulong campaignId) => Result(await service.RegisterIndividual(Actor, campaignId), campaignId);
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Create(ulong campaignId, string name) => Result(await service.CreateGroup(Actor, campaignId, name), campaignId);
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Leave(ulong campaignId) => Result(await service.Leave(Actor, campaignId), campaignId);
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Invite(ulong groupId, string studentId) => await GroupResult(groupId, () => service.Invite(Actor, groupId, studentId));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Respond(ulong groupId, bool accept) => await GroupResult(groupId, () => service.Respond(Actor, groupId, accept));
    private async Task<IActionResult> GroupResult(ulong groupId, Func<Task<string?>> action) {
        var group = await db.Set<ProjectRegistrationGroup>().AsNoTracking().SingleOrDefaultAsync(g => g.Id == groupId);
        return group == null ? NotFound() : Result(await action(), group.CampaignId);
    }
}
