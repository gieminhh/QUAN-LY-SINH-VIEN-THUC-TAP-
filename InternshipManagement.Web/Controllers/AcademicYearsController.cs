
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.ViewModels.AcademicYears;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Web.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.TrainingManager}")]
public class AcademicYearsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AcademicYearsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // 1. Danh sách năm học
    public async Task<IActionResult> Index(
        string? search,
        int page = 1)
    {
        int pageSize = 10;
        page = Math.Max(page, 1);

        var query = _context.AcademicYears
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(y => y.Code.Contains(search));
        }

        int totalCount = await query.CountAsync();

        var academicYears = await query
            .OrderByDescending(y => y.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(y => new AcademicYearItemViewModel
            {
                Id = y.Id,
                Code = y.Code,
                StartDate = y.StartDate,
                EndDate = y.EndDate
            })
            .ToListAsync();

        var model = new AcademicYearListViewModel
        {
            AcademicYears = academicYears,
            Search = search,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return View(model);
    }

    // 2. Thêm năm học - GET
    [HttpGet]
    public IActionResult Create()
    {
        return View(new AcademicYearCreateEditViewModel
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(10)
        });
    }

    // 3. Thêm năm học - POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        AcademicYearCreateEditViewModel model)
    {
        model.Code = model.Code?.Trim() ?? "";

        if (model.EndDate <= model.StartDate)
        {
            ModelState.AddModelError(
                nameof(model.EndDate),
                "Ngày kết thúc phải sau ngày bắt đầu.");
        }

        bool codeExists = await _context.AcademicYears
            .AnyAsync(y => y.Code == model.Code);

        if (codeExists)
        {
            ModelState.AddModelError(
                nameof(model.Code),
                "Mã năm học đã tồn tại.");
        }

        if (!ModelState.IsValid)
            return View(model);

        var academicYear = new AcademicYear
        {
            Code = model.Code,
            StartDate = model.StartDate,
            EndDate = model.EndDate
        };

        _context.AcademicYears.Add(academicYear);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Thêm năm học thành công.";

        return RedirectToAction(nameof(Index));
    }

    // 4. Chỉnh sửa năm học - GET
    [HttpGet]
    public async Task<IActionResult> Edit(ulong id)
    {
        var academicYear = await _context.AcademicYears
            .FirstOrDefaultAsync(y => y.Id == id);

        if (academicYear == null)
            return NotFound();

        var model = new AcademicYearCreateEditViewModel
        {
            Id = academicYear.Id,
            Code = academicYear.Code,
            StartDate = academicYear.StartDate,
            EndDate = academicYear.EndDate
        };

        return View(model);
    }

    // 5. Chỉnh sửa năm học - POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        AcademicYearCreateEditViewModel model)
    {
        model.Code = model.Code?.Trim() ?? "";

        var academicYear = await _context.AcademicYears
            .FirstOrDefaultAsync(y => y.Id == model.Id);

        if (academicYear == null)
            return NotFound();

        if (model.EndDate <= model.StartDate)
        {
            ModelState.AddModelError(
                nameof(model.EndDate),
                "Ngày kết thúc phải sau ngày bắt đầu.");
        }

        bool codeExists = await _context.AcademicYears
            .AnyAsync(y => y.Code == model.Code && y.Id != model.Id);

        if (codeExists)
        {
            ModelState.AddModelError(
                nameof(model.Code),
                "Mã năm học đã được sử dụng.");
        }

        if (await _context.Terms.AnyAsync(t => t.AcademicYearId == model.Id && (t.StartDate < model.StartDate || t.EndDate > model.EndDate)))
            ModelState.AddModelError(nameof(model.EndDate), "Khoảng ngày năm học phải bao phủ tất cả học kỳ hiện có.");

        if (!ModelState.IsValid)
            return View(model);

        academicYear.Code = model.Code;
        academicYear.StartDate = model.StartDate;
        academicYear.EndDate = model.EndDate;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Cập nhật năm học thành công.";

        return RedirectToAction(nameof(Index));
    }
}
