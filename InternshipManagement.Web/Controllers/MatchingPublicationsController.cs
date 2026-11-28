using System.Data.Common;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels.Assignments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Web.Controllers;

[Authorize(Policy = "RequireCampaignManager")]
public sealed class MatchingPublicationsController(ApplicationDbContext db, MatchingPublicationService service,
    IUserScopeService scopes, IDataProtectionProvider protection, ILogger<MatchingPublicationsController> logger) : Controller
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    private readonly ITimeLimitedDataProtector protector = protection.CreateProtector("MatchingPublication.v1").ToTimeLimitedDataProtector();
    private record Receipt(string Actor, ulong CampaignId, string Fingerprint);

    [HttpGet]
    public async Task<IActionResult> Index(ulong? campaignId)
    {
        var campaigns = await (await scopes.FilterReviewableCampaignsAsync(Actor, db.Campaigns.AsNoTracking())).OrderByDescending(c => c.Id).ToListAsync();
        var campaign = campaignId.HasValue ? campaigns.Find(c => c.Id == campaignId) : campaigns.FirstOrDefault();
        if (campaignId.HasValue && campaign is null) return Forbid();
        var page = new PublicationReviewPage { Campaigns = campaigns, Campaign = campaign, UtcNow = service.UtcNow };
        if (campaign is null) return View(page);
        if (!await service.CanManage(Actor, campaign.Id)) return Forbid();
        page.Plan = await service.Review(campaign.Id);
        page.Rounds = await db.Set<MatchingRound>().AsNoTracking().Where(r => r.CampaignId == campaign.Id).OrderByDescending(r => r.Number).ToListAsync();
        page.CanPublish = page.Plan!.CanPublish(service.UtcNow) && page.Rounds.FirstOrDefault()?.State != MatchingRoundState.Published;
        page.Token = protector.Protect(JsonSerializer.Serialize(new Receipt(Actor, campaign.Id, page.Plan.Input.Fingerprint)), TimeSpan.FromMinutes(30));
        return View(page);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(ulong campaignId, string? token)
    {
        if (!await service.CanManage(Actor, campaignId)) return Forbid();
        Receipt? receipt;
        try { receipt = JsonSerializer.Deserialize<Receipt>(protector.Unprotect(token ?? "")); }
        catch (Exception ex) when (ex is CryptographicException or ArgumentException or JsonException)
        { return BadRequest("Phiên duyệt không hợp lệ hoặc đã hết hạn. Vui lòng tải lại."); }
        if (receipt?.Actor != Actor || receipt.CampaignId != campaignId) return BadRequest();
        return await Change(campaignId, () => service.Publish(Actor, campaignId, receipt.Fingerprint), "Đã duyệt và công bố kết quả cho sinh viên.");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OpenSupplementary(ulong campaignId, ulong previousRoundId, DateTime closesAt)
    {
        if (!await service.CanManage(Actor, campaignId)) return Forbid();
        if (!ModelState.IsValid) return BadRequest("Hạn bổ sung không hợp lệ.");
        // datetime-local is explicitly presented as Vietnam time in the view.
        var utc = DateTime.SpecifyKind(closesAt.AddHours(-7), DateTimeKind.Utc);
        return await Change(campaignId, () => service.OpenSupplementary(Actor, campaignId, previousRoundId, utc), "Đã mở đăng ký nguyện vọng bổ sung.");
    }

    private async Task<IActionResult> Change(ulong campaignId, Func<Task<string?>> action, string success)
    {
        string? error;
        try { error = await action(); }
        catch (Exception ex) when (ex is DbException or DbUpdateException)
        {
            logger.LogWarning(ex, "Matching publication change failed for campaign {CampaignId}", campaignId);
            error = "Chưa lưu được thay đổi. Vui lòng tải lại và thử lại.";
        }
        TempData[error is null ? "SuccessMessage" : "ErrorMessage"] = error ?? success;
        return RedirectToAction(nameof(Index), new { campaignId });
    }
}
