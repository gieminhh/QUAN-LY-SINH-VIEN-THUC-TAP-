using System.Data.Common;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels.Assignments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Web.Controllers;

[Authorize(Policy = "RequireCampaignManager")]
public class MatchingPreviewController(IMatchingPreviewService service, IUserScopeService scopes,
    IDataProtectionProvider protection, ILogger<MatchingPreviewController> logger) : Controller
{
    private readonly ITimeLimitedDataProtector protector = protection.CreateProtector("MatchingPreview.v1").ToTimeLimitedDataProtector();
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private record PreviewReceipt(ulong CampaignId, string ActorId, int Seed, string Fingerprint);

    [HttpGet]
    public async Task<IActionResult> Index(ulong id)
    {
        if (!await scopes.CanReviewCampaignAsync(Actor, id)) return Forbid();
        var preview = await service.PreviewAsync(id);
        if (preview is null) return NotFound();
        var receipt = new PreviewReceipt(id, Actor, preview.Input.Seed, preview.Input.Fingerprint);
        return View(new MatchingPreviewViewModel(preview,
            protector.Protect(JsonSerializer.Serialize(receipt), TimeSpan.FromMinutes(30))));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(ulong id, string? token)
    {
        // Recheck scope on every POST, even when a user has a valid old preview token.
        if (!await scopes.CanReviewCampaignAsync(Actor, id)) return Forbid();
        PreviewReceipt? receipt;
        try
        {
            receipt = JsonSerializer.Deserialize<PreviewReceipt>(protector.Unprotect(token ?? string.Empty));
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or ArgumentException)
        {
            return BadRequest("Phiên xem trước không hợp lệ hoặc đã hết hạn. Vui lòng tải lại.");
        }
        if (receipt is null || receipt.CampaignId != id || receipt.ActorId != Actor) return BadRequest();
        try
        {
            var result = await service.ConfirmAsync(id, receipt.Seed, receipt.Fingerprint);
            TempData[result.Ok ? "SuccessMessage" : "ErrorMessage"] = result.Ok
                ? "Đã xác nhận và lưu kết quả ghép. Kết quả chưa được công bố."
                : result.Error;
        }
        catch (Exception ex) when (ex is DbUpdateException or DbException)
        {
            logger.LogWarning(ex, "Concurrent matching confirmation failed for campaign {CampaignId}", id);
            TempData["ErrorMessage"] = "Chưa lưu được kết quả. Vui lòng tải lại bản xem trước và thử lại.";
        }
        return RedirectToAction(nameof(Index), new { id });
    }
}
