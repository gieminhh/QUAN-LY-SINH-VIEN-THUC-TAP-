using System.Security.Claims;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Web.Controllers;

[Authorize]
public class InternshipPositionsController : Controller
{
    private readonly ApplicationDbContext _context;

    public InternshipPositionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: InternshipPositions
    public async Task<IActionResult> Index(ulong? campaignId, string? status, string? search)
    {
        var query = _context.InternshipPositions
            .AsNoTracking()
            .Include(p => p.Campaign)
            .Include(p => p.InternshipPlace)
            .Include(p => p.ReviewedByUser)
            .AsQueryable();

        if (campaignId.HasValue && campaignId.Value > 0)
        {
            query = query.Where(p => p.CampaignId == campaignId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "ALL")
        {
            var normalized = status.Trim().ToUpperInvariant();
            query = query.Where(p => p.ApprovalStatus == normalized);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(p =>
                p.Title.ToLower().Contains(keyword) ||
                (p.Skills != null && p.Skills.ToLower().Contains(keyword)) ||
                (p.InternshipPlace != null && p.InternshipPlace.Name.ToLower().Contains(keyword)));
        }

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var campaigns = await _context.Campaigns
            .AsNoTracking()
            .OrderByDescending(c => c.Id)
            .ToListAsync();

        ViewBag.Campaigns = new SelectList(campaigns, "Id", "Name", campaignId);
        ViewBag.SelectedCampaignId = campaignId;
        ViewBag.SelectedStatus = string.IsNullOrWhiteSpace(status) ? "ALL" : status.ToUpperInvariant();
        ViewBag.Search = search;

        return View(items);
    }

    // GET: InternshipPositions/Create
    public async Task<IActionResult> Create()
    {
        await LoadFormSelectListsAsync();
        return View(new InternshipPosition
        {
            Slots = 1,
            IsOpen = true,
            ApprovalStatus = InternshipPositionApprovalStatuses.Pending
        });
    }

    // POST: InternshipPositions/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InternshipPosition model)
    {
        NormalizeModel(model);

        if (!ModelState.IsValid)
        {
            await LoadFormSelectListsAsync(model.CampaignId, model.InternshipPlaceId);
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        model.CreatedByUserId = userId;
        model.ApprovalStatus = InternshipPositionApprovalStatuses.Pending;
        model.ReviewNote = null;
        model.ReviewedByUserId = null;
        model.ReviewedAt = null;
        model.CreatedAt = DateTime.UtcNow;
        model.UpdatedAt = DateTime.UtcNow;

        _context.InternshipPositions.Add(model);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã đăng vị trí \"{model.Title}\", đang chờ Khoa xét duyệt.";
        return RedirectToAction(nameof(Index));
    }

    // GET: InternshipPositions/Edit/5
    public async Task<IActionResult> Edit(ulong id)
    {
        var position = await _context.InternshipPositions.FindAsync(id);
        if (position == null)
        {
            return NotFound();
        }

        await LoadFormSelectListsAsync(position.CampaignId, position.InternshipPlaceId);
        return View(position);
    }

    // POST: InternshipPositions/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ulong id, InternshipPosition model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var existing = await _context.InternshipPositions.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        NormalizeModel(model);

        if (!ModelState.IsValid)
        {
            await LoadFormSelectListsAsync(model.CampaignId, model.InternshipPlaceId);
            return View(model);
        }

        existing.CampaignId = model.CampaignId;
        existing.InternshipPlaceId = model.InternshipPlaceId;
        existing.Title = model.Title;
        existing.Description = model.Description;
        existing.Requirements = model.Requirements;
        existing.Benefits = model.Benefits;
        existing.Skills = model.Skills;
        existing.Slots = model.Slots;
        existing.Deadline = model.Deadline;
        existing.IsOpen = model.IsOpen;
        existing.UpdatedAt = DateTime.UtcNow;

        // Nếu chỉnh sửa lại sau khi bị từ chối/yêu cầu sửa thì đưa về chờ duyệt lại
        if (existing.ApprovalStatus == InternshipPositionApprovalStatuses.Rejected)
        {
            existing.ApprovalStatus = InternshipPositionApprovalStatuses.Pending;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã cập nhật vị trí thực tập \"{existing.Title}\".";
        return RedirectToAction(nameof(Index));
    }

    // POST: InternshipPositions/Review/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(ulong id, string decision, string? reviewNote)
    {
        var position = await _context.InternshipPositions.FindAsync(id);
        if (position == null)
        {
            return NotFound();
        }

        var normalized = (decision ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized != InternshipPositionApprovalStatuses.Approved &&
            normalized != InternshipPositionApprovalStatuses.Rejected)
        {
            TempData["ErrorMessage"] = "Quyết định xét duyệt không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        position.ApprovalStatus = normalized;
        position.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? null : reviewNote.Trim();
        position.ReviewedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        position.ReviewedAt = DateTime.UtcNow;
        position.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var label = InternshipPositionApprovalStatuses.GetLabel(normalized);
        TempData["SuccessMessage"] = $"Đã cập nhật trạng thái vị trí \"{position.Title}\": {label}.";
        return RedirectToAction(nameof(Index));
    }

    private void NormalizeModel(InternshipPosition model)
    {
        model.Title = model.Title?.Trim() ?? string.Empty;
        model.Description = model.Description?.Trim() ?? string.Empty;
        model.Requirements = string.IsNullOrWhiteSpace(model.Requirements) ? null : model.Requirements.Trim();
        model.Benefits = string.IsNullOrWhiteSpace(model.Benefits) ? null : model.Benefits.Trim();
        model.Skills = string.IsNullOrWhiteSpace(model.Skills) ? null : model.Skills.Trim();
    }

    private async Task LoadFormSelectListsAsync(ulong? selectedCampaignId = null, ulong? selectedPlaceId = null)
    {
        var campaigns = await _context.Campaigns
            .AsNoTracking()
            .OrderByDescending(c => c.Id)
            .ToListAsync();

        var places = await _context.InternshipPlaces
            .AsNoTracking()
            .Where(p => p.IsActive && p.CooperationStatus != BusinessCooperationStatuses.Terminated)
            .OrderBy(p => p.Name)
            .ToListAsync();

        ViewBag.Campaigns = new SelectList(campaigns, "Id", "Name", selectedCampaignId);
        ViewBag.InternshipPlaces = new SelectList(places, "Id", "Name", selectedPlaceId);
    }
}