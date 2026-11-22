using System.Security.Claims;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace InternshipManagement.Web.Controllers;
[Authorize(Roles = AppRoles.Lecturer)]
public sealed class LecturerPrioritiesController(ApplicationDbContext db, ParticipationService service) : Controller
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private async Task<RankingPage?> Page(ulong? topicId) {
        if (!await service.HasRole(Actor, AppRoles.Lecturer)) return null;
        var topics = await db.Topics.AsNoTracking().Where(t => t.CreatedByUserId == Actor ||
            db.TopicLecturers.Any(l => l.TopicId == t.Id && l.LecturerUserId == Actor)).OrderByDescending(t => t.Id).ToListAsync();
        var topic = topicId.HasValue ? topics.Find(t => t.Id == topicId) : topics.FirstOrDefault();
        if (topicId.HasValue && topic == null) return null;
        var page = new RankingPage { Topics = topics };
        if (topic == null) return page;
        var campaign = await db.Campaigns.AsNoTracking().SingleAsync(c => c.Id == topic.CampaignId);
        var list = await db.Set<LecturerPriorityList>().AsNoTracking().SingleOrDefaultAsync(l => l.TopicId == topic.Id && l.LecturerId == Actor);
        page.Input = new() { TopicId = topic.Id, Version = list?.Version, Reason = list?.Reason ?? "",
            StudentIds = await db.Set<LecturerStudentPriority>().Where(p => p.TopicId == topic.Id && p.LecturerId == Actor).OrderBy(p => p.Rank).Select(p => p.StudentId).ToListAsync() };
        page.Candidates = await service.Candidates(topic).OrderBy(u => u.FullName).ThenBy(u => u.Id).ToListAsync();
        page.SavedAt = list?.SavedAt;
        page.LockedReason = await service.Window(campaign, true);
        if (topic.Status != TopicStatuses.Published || topic.ApprovalStatus != TopicApprovalStatuses.Approved) page.LockedReason = "Đề tài chưa được duyệt và công khai.";
        return page;
    }
    [HttpGet] public async Task<IActionResult> Index(ulong? topicId) {
        var page = await Page(topicId); return page == null ? Forbid() : View(page);
    }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Save([Bind(Prefix="Input")] RankingInput input) {
        var page = await Page(input.TopicId); if (page == null) return Forbid();
        if (ModelState.IsValid) {
            var error = await service.SaveRanking(Actor, input.TopicId, input.StudentIds, input.Version, input.Reason);
            if (error == null) { TempData["SuccessMessage"] = "Đã lưu thứ tự ưu tiên."; return RedirectToAction(nameof(Index), new { topicId = input.TopicId }); }
            ModelState.AddModelError("", error);
        }
        if (page.LockedReason == null) page.Input = input;
        return View(nameof(Index), page);
    }
}
