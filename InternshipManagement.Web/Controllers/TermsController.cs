
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.ViewModels.Terms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Web.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.TrainingManager}")]
public class TermsController : Controller
{
    private readonly ApplicationDbContext _context;

    public TermsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        int pageSize = 10;
        page = Math.Max(page, 1);

        var query = _context.Terms
            .AsNoTracking()
            .Include(t => t.AcademicYear)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(t =>
                t.AcademicYear != null &&
                t.AcademicYear.Code.Contains(search)
                || t.TermNo.ToString().Contains(search));
        }

        int totalCount = await query.CountAsync();

        var terms = await query
            .OrderByDescending(t => t.AcademicYear!.StartDate)
            .ThenBy(t => t.TermNo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TermItemViewModel
            {
                Id = t.Id,
                AcademicYearId = t.AcademicYearId,
                AcademicYearCode = t.AcademicYear!.Code,
                TermNo = t.TermNo,
                StartDate = t.StartDate,
                EndDate = t.EndDate
            })
            .ToListAsync();

        var model = new TermListViewModel
        {
            Terms = terms,
            Search = search,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new TermCreateEditViewModel
        {
            TermNo = 1,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(4)
        };

        await LoadAcademicYears(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TermCreateEditViewModel model)
    {
        model.StartDate = model.StartDate.Date;
        model.EndDate = model.EndDate.Date;

        await ValidateTerm(model);

        if (!ModelState.IsValid)
        {
            await LoadAcademicYears(model);
            return View(model);
        }

        var term = new Term
        {
            AcademicYearId = model.AcademicYearId,
            TermNo = model.TermNo,
            StartDate = model.StartDate,
            EndDate = model.EndDate
        };

        _context.Terms.Add(term);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Thêm học kỳ thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(ulong id)
    {
        var term = await _context.Terms
            .FirstOrDefaultAsync(t => t.Id == id);

        if (term == null)
            return NotFound();

        var model = new TermCreateEditViewModel
        {
            Id = term.Id,
            AcademicYearId = term.AcademicYearId,
            TermNo = term.TermNo,
            StartDate = term.StartDate,
            EndDate = term.EndDate
        };

        await LoadAcademicYears(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TermCreateEditViewModel model)
    {
        model.StartDate = model.StartDate.Date;
        model.EndDate = model.EndDate.Date;

        var term = await _context.Terms
            .FirstOrDefaultAsync(t => t.Id == model.Id);

        if (term == null)
            return NotFound();

        await ValidateTerm(model, model.Id);

        if (!ModelState.IsValid)
        {
            await LoadAcademicYears(model);
            return View(model);
        }

        term.AcademicYearId = model.AcademicYearId;
        term.TermNo = model.TermNo;
        term.StartDate = model.StartDate;
        term.EndDate = model.EndDate;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Cập nhật học kỳ thành công.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateTerm(
        TermCreateEditViewModel model,
        ulong? currentId = null)
    {
            // Kiểm tra số học kỳ
        if (model.TermNo < 1 || model.TermNo > 3)
        {
            ModelState.AddModelError(
                nameof(model.TermNo),
                "Số học kỳ chỉ được từ 1 đến 3.");
        }

        // Kiểm tra ngày tháng
        if (model.EndDate <= model.StartDate)
        {
            ModelState.AddModelError(
                nameof(model.EndDate),
                "Ngày kết thúc phải sau ngày bắt đầu.");
        }

        var academicYear = await _context.AcademicYears
            .FirstOrDefaultAsync(y => y.Id == model.AcademicYearId);

        if (academicYear == null)
        {
            ModelState.AddModelError(
                nameof(model.AcademicYearId),
                "Năm học được chọn không tồn tại.");
        }
        else
        {
            if (model.TermNo == 3)
            {
                if (model.StartDate <= academicYear.EndDate.Date)
                {
                    ModelState.AddModelError(
                        nameof(model.StartDate),
                        "Học kỳ hè phải bắt đầu sau khi năm học kết thúc.");
                }
            }
            else
            {
                if (model.StartDate < academicYear.StartDate.Date ||
                    model.EndDate > academicYear.EndDate.Date)
                {
                    ModelState.AddModelError(
                        nameof(model.StartDate),
                        "Thời gian học kỳ phải nằm trong thời gian của năm học.");
                }
            }
        }

        bool termExists = await _context.Terms.AnyAsync(t =>
            t.AcademicYearId == model.AcademicYearId &&
            t.TermNo == model.TermNo &&
            (!currentId.HasValue || t.Id != currentId.Value));

        if (termExists)
        {
            ModelState.AddModelError(
                nameof(model.TermNo),
                "Học kỳ này đã tồn tại trong năm học được chọn.");
        }
    }
    private async Task LoadAcademicYears(TermCreateEditViewModel model)
    {
        model.AcademicYears = await _context.AcademicYears
            .AsNoTracking()
            .OrderByDescending(y => y.StartDate)
            .Select(y => new SelectListItem
            {
                Value = y.Id.ToString(),
                Text = y.Code
            })
            .ToListAsync();
    }
}
