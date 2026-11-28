using System.Security.Claims;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Web.Controllers;

[Authorize]
public class InternshipPlacesController : Controller
{
    private readonly ApplicationDbContext _context;

    public InternshipPlacesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: InternshipPlaces
    public async Task<IActionResult> Index(string? search, string? status, bool? isActive)
    {
        var query = _context.InternshipPlaces
            .AsNoTracking()
            .Include(p => p.Registrations)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(keyword) ||
                (p.TaxCode != null && p.TaxCode.ToLower().Contains(keyword)) ||
                (p.Address != null && p.Address.ToLower().Contains(keyword)) ||
                (p.ContactPerson != null && p.ContactPerson.ToLower().Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "ALL")
        {
            var normalized = status.Trim().ToUpperInvariant();
            query = query.Where(p => p.CooperationStatus == normalized);
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        var items = await query
            .OrderByDescending(p => p.CooperationStatus == BusinessCooperationStatuses.ActivePartner)
            .ThenBy(p => p.Name)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Status = string.IsNullOrWhiteSpace(status) ? "ALL" : status.ToUpperInvariant();
        ViewBag.IsActive = isActive;

        return View(items);
    }

    // GET: InternshipPlaces/Create
    public IActionResult Create()
    {
        var model = new InternshipPlace
        {
            CooperationStatus = BusinessCooperationStatuses.ActivePartner,
            IsActive = true
        };
        return View(model);
    }

    // POST: InternshipPlaces/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InternshipPlace model)
    {
        NormalizeAndValidatePlace(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        model.CreatedByUserId = userId;
        model.CreatedAt = DateTime.UtcNow;
        model.UpdatedAt = DateTime.UtcNow;
        model.IsActive = model.CooperationStatus == BusinessCooperationStatuses.ActivePartner
                         || model.CooperationStatus == BusinessCooperationStatuses.PendingVerify;

        _context.InternshipPlaces.Add(model);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã thêm doanh nghiệp \"{model.Name}\" vào danh mục.";
        return RedirectToAction(nameof(Index));
    }

    // GET: InternshipPlaces/Edit/5
    public async Task<IActionResult> Edit(ulong id)
    {
        var place = await _context.InternshipPlaces.FindAsync(id);
        if (place == null)
        {
            return NotFound();
        }
        return View(place);
    }

    // POST: InternshipPlaces/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ulong id, InternshipPlace model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var existing = await _context.InternshipPlaces.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        NormalizeAndValidatePlace(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        existing.Name = model.Name;
        existing.TaxCode = model.TaxCode;
        existing.Address = model.Address;
        existing.Website = model.Website;
        existing.ContactPerson = model.ContactPerson;
        existing.ContactPhone = model.ContactPhone;
        existing.ContactEmail = model.ContactEmail;
        existing.DefaultPosition = model.DefaultPosition;
        existing.CooperationStatus = model.CooperationStatus;
        existing.CooperationNote = model.CooperationNote;
        existing.IsActive = model.CooperationStatus == BusinessCooperationStatuses.ActivePartner
                            || model.CooperationStatus == BusinessCooperationStatuses.PendingVerify;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã cập nhật thông tin doanh nghiệp \"{existing.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    // POST: InternshipPlaces/UpdateStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(ulong id, string cooperationStatus, string? cooperationNote)
    {
        var place = await _context.InternshipPlaces.FindAsync(id);
        if (place == null)
        {
            return NotFound();
        }

        var normalized = (cooperationStatus ?? string.Empty).Trim().ToUpperInvariant();
        if (!BusinessCooperationStatuses.All.Contains(normalized))
        {
            TempData["ErrorMessage"] = "Trạng thái hợp tác không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        place.CooperationStatus = normalized;
        if (!string.IsNullOrWhiteSpace(cooperationNote))
        {
            place.CooperationNote = cooperationNote.Trim();
        }
        place.IsActive = normalized == BusinessCooperationStatuses.ActivePartner
                         || normalized == BusinessCooperationStatuses.PendingVerify;
        place.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã chuyển trạng thái \"{place.Name}\" sang \"{BusinessCooperationStatuses.GetLabel(normalized)}\".";
        return RedirectToAction(nameof(Index));
    }

    private void NormalizeAndValidatePlace(InternshipPlace model)
    {
        model.Name = model.Name?.Trim() ?? string.Empty;
        model.TaxCode = string.IsNullOrWhiteSpace(model.TaxCode) ? null : model.TaxCode.Trim();
        model.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
        model.Website = string.IsNullOrWhiteSpace(model.Website) ? null : model.Website.Trim();
        model.ContactPerson = string.IsNullOrWhiteSpace(model.ContactPerson) ? null : model.ContactPerson.Trim();
        model.ContactPhone = string.IsNullOrWhiteSpace(model.ContactPhone) ? null : model.ContactPhone.Trim();
        model.ContactEmail = string.IsNullOrWhiteSpace(model.ContactEmail) ? null : model.ContactEmail.Trim();
        model.DefaultPosition = string.IsNullOrWhiteSpace(model.DefaultPosition) ? null : model.DefaultPosition.Trim();
        model.CooperationNote = string.IsNullOrWhiteSpace(model.CooperationNote) ? null : model.CooperationNote.Trim();
        model.CooperationStatus = string.IsNullOrWhiteSpace(model.CooperationStatus)
            ? BusinessCooperationStatuses.ActivePartner
            : model.CooperationStatus.Trim().ToUpperInvariant();

        if (!BusinessCooperationStatuses.All.Contains(model.CooperationStatus))
        {
            ModelState.AddModelError(nameof(model.CooperationStatus), "Vui lòng chọn tình trạng hợp tác hợp lệ.");
        }
    }
}