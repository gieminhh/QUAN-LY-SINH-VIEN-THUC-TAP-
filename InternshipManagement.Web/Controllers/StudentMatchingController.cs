using System.Data.Common;
using System.Security.Claims;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels.Assignments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Student)]
public sealed class StudentMatchingController(ApplicationDbContext db, MatchingPublicationService service,
    ILogger<StudentMatchingController> logger) : Controller
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    [HttpGet]
    public async Task<IActionResult> Index(ulong? campaignId)
    {
        if (!await service.HasRole(Actor, AppRoles.Student)) return Forbid();
        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == Actor);
        var campaigns = await db.Campaigns.AsNoTracking().Where(c => c.MajorId == user.MajorId ||
            db.Set<MatchingRound>().Any(r => r.CampaignId == c.Id && r.State == MatchingRoundState.Published &&
                db.Set<MatchingPublishedResult>().Any(p => p.RoundId == r.Id && p.StudentId == Actor)))
            .OrderByDescending(c => c.Id).ToListAsync();
        var campaign = campaignId.HasValue ? campaigns.Find(c => c.Id == campaignId) : campaigns.FirstOrDefault();
        if (campaignId.HasValue && campaign is null) return NotFound();
        var page = new StudentMatchingPage { Campaigns = campaigns, Campaign = campaign };
        if (campaign is null) return View(page);
        page.PublishedRound = await db.Set<MatchingRound>().AsNoTracking().Where(r => r.CampaignId == campaign.Id && r.State == MatchingRoundState.Published)
            .OrderByDescending(r => r.Number).FirstOrDefaultAsync();
        if (page.PublishedRound is not null)
            page.Result = await db.Set<MatchingPublishedResult>().AsNoTracking().SingleOrDefaultAsync(r => r.RoundId == page.PublishedRound.Id && r.StudentId == Actor);
        page.SupplementaryRound = await db.Set<MatchingRound>().AsNoTracking().Where(r => r.CampaignId == campaign.Id && r.State == MatchingRoundState.RegistrationOpen)
            .OrderByDescending(r => r.Number).FirstOrDefaultAsync();
        page.CanRegister = page.SupplementaryRound is not null && campaign.Status == CampaignStatuses.Closed && user.MajorId == campaign.MajorId &&
            service.UtcNow < page.SupplementaryRound.ClosesAt && page.Result?.Status != MatchStatus.Matched &&
            !await db.TopicStudentAssignments.AnyAsync(a => a.CampaignId == campaign.Id && a.StudentUserId == Actor && a.Status == MatchStatus.Matched);
        if (page.SupplementaryRound is not null)
        {
            page.Preferences = await db.Set<MatchingRoundPreference>().AsNoTracking().Where(p => p.RoundId == page.SupplementaryRound.Id && p.StudentId == Actor)
                .OrderBy(p => p.Priority).ToListAsync();
            page.PreferenceVersion = MatchingPublicationService.PreferenceVersion(page.Preferences);
            page.Topics = await service.AvailableTopics(campaign.Id).OrderBy(t => t.Title)
                .Select(t => new SupplementaryTopic(t.Id, t.Title, t.MaxStudents.HasValue ? t.MaxStudents -
                    db.TopicStudentAssignments.Count(a => a.TopicId == t.Id && a.Status == MatchStatus.Matched) : null)).ToListAsync();
        }
        return View(page);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Confirm(ulong campaignId, ulong roundId, ulong topicId) =>
        Change(campaignId, () => service.Confirm(Actor, roundId, topicId), "Đã xác nhận cặp ghép của bạn.");

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> SavePreferences(ulong campaignId, ulong roundId, List<ulong> topicIds, string version) =>
        Change(campaignId, () => service.SavePreferences(Actor, roundId, topicIds.Where(t => t != 0).ToList(), version), "Đã lưu nguyện vọng bổ sung.");

    private async Task<IActionResult> Change(ulong campaignId, Func<Task<string?>> action, string success)
    {
        if (!await service.HasRole(Actor, AppRoles.Student)) return Forbid();
        if (!ModelState.IsValid) return BadRequest();
        string? error;
        try { error = await action(); }
        catch (Exception ex) when (ex is DbException or DbUpdateException)
        {
            logger.LogWarning(ex, "Student matching update failed");
            error = "Chưa lưu được thay đổi. Vui lòng tải lại và thử lại.";
        }
        TempData[error is null ? "SuccessMessage" : "ErrorMessage"] = error ?? success;
        return RedirectToAction(nameof(Index), new { campaignId });
    }
}
