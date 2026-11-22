using System.Security.Claims;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace InternshipManagement.Web.Controllers;
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.TrainingManager},{AppRoles.FacultyManager}")]
public sealed class ParticipationPoliciesController(ApplicationDbContext db, ParticipationService service, IUserScopeService scopes) : Controller
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private async Task<PolicyPage> Page(ulong? id)
    {
        var campaigns = await (await scopes.FilterAccessibleCampaignsAsync(Actor, db.Campaigns)).OrderByDescending(c => c.Id).ToListAsync();
        var c = id.HasValue ? campaigns.Find(c => c.Id == id) : campaigns.FirstOrDefault();
        return new PolicyPage { Campaigns = campaigns, Input = c == null ? new() :
            await db.Set<ParticipationPolicy>().AsNoTracking().SingleOrDefaultAsync(p => p.CampaignId == c.Id) ?? new() {
                CampaignId = c.Id, RegistrationOpensAt = c.StartDate, RegistrationClosesAt = c.EndDate.Date.AddDays(1).AddSeconds(-1),
                RankingOpensAt = c.StartDate, RankingClosesAt = c.EndDate.Date.AddDays(1).AddSeconds(-1) } };
    }
    [HttpGet] public async Task<IActionResult> Index(ulong? campaignId)
    {
        if (!await service.HasManagerRole(Actor)) return Forbid();
        var page = await Page(campaignId);
        return campaignId.HasValue && page.Input.CampaignId != campaignId ? Forbid() : View(page);
    }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Save([Bind(Prefix="Input")] ParticipationPolicy input)
    {
        if (!await service.HasManagerRole(Actor) || !await scopes.CanManageCampaignAsync(Actor, input.CampaignId)) return Forbid();
        if (ModelState.IsValid) {
            var error = await service.SavePolicy(Actor, input);
            if (error == null) { TempData["SuccessMessage"] = "Đã lưu chính sách đăng ký và xếp hạng."; return RedirectToAction(nameof(Index), new { campaignId = input.CampaignId }); }
            ModelState.AddModelError("", error);
        }
        var page = await Page(input.CampaignId); page.Input = input; return View(nameof(Index), page);
    }
}
