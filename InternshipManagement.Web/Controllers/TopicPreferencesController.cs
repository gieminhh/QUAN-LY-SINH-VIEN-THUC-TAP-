using System.Security.Claims;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Student)]
public sealed class TopicPreferencesController(TopicPreferenceService preferences) : Controller
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(ulong? campaignId)
    {
        var page = await preferences.GetPageAsync(Actor, campaignId);
        return page == null ? NotFound() : View(page);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([Bind(Prefix = "Input")] TopicPreferenceInput input)
    {
        if (ModelState.IsValid)
        {
            var error = await preferences.SaveAsync(Actor, input);
            if (error == null)
            {
                TempData["SuccessMessage"] = "Đã lưu nguyện vọng theo thứ tự ưu tiên.";
                return RedirectToAction(nameof(Index), new { campaignId = input.CampaignId });
            }
            ModelState.AddModelError(string.Empty, error);
        }

        var page = await preferences.GetPageAsync(Actor, input.CampaignId);
        if (page == null) return NotFound();
        if (page.LockedReason == null) page.Input = input;
        return View(nameof(Index), page);
    }
}
