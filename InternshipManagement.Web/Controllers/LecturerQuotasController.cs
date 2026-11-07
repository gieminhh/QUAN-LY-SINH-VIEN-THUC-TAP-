using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels;

namespace InternshipManagement.Web.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.TrainingManager},{AppRoles.FacultyManager}")]
public class LecturerQuotasController : Controller
{
    private readonly ILecturerProfileService _lecturerService;
    private readonly IUserScopeService _scopeService;
    private readonly ApplicationDbContext _context;

    public LecturerQuotasController(
        ILecturerProfileService lecturerService,
        IUserScopeService scopeService,
        ApplicationDbContext context)
    {
        _lecturerService = lecturerService;
        _scopeService = scopeService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(ulong? campaignId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();

        var accessibleCampaignsQuery = await _scopeService.FilterAccessibleCampaignsAsync(currentUserId, _context.Campaigns);
        var campaigns = await accessibleCampaignsQuery
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        if (!campaigns.Any())
        {
            TempData["ErrorMessage"] = "Bạn chưa được phân quyền quản lý đợt thực tập / đồ án nào.";
            return RedirectToAction("Index", "Home");
        }

        var selectedCampaignId = campaignId.HasValue && campaignId.Value > 0
            ? campaignId.Value
            : campaigns.First().Id;

        // Verify scope over this campaign
        var canManage = await _scopeService.CanManageCampaignAsync(currentUserId, selectedCampaignId);
        if (!canManage)
        {
            return Forbid();
        }

        var model = await _lecturerService.GetQuotaListViewModelAsync(selectedCampaignId, currentUserId);
        model.CampaignOptions = new SelectList(campaigns.Select(c => new { c.Id, Display = $"{c.Code} - {c.Name}" }), "Id", "Display", selectedCampaignId);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetQuota(SetQuotaInputModel model)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();

        if(!ModelState.IsValid)return BadRequest("Hạn mức không hợp lệ.");
        var result = await _lecturerService.SetQuotaAsync(model.LecturerProfileId, model.CampaignId, model.MaxStudents, currentUserId);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = "Cập nhật hạn mức hướng dẫn sinh viên cho giảng viên thành công!";
        }

        return RedirectToAction(nameof(Index), new { campaignId = model.CampaignId });
    }
}
