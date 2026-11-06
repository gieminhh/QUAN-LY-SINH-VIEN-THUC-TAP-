using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.ViewModels.Majors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace InternshipManagement.Web.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.TrainingManager}")]
public class MajorsController : Controller
{
    private readonly ApplicationDbContext _context;

    public MajorsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // 1. Danh sách ngành
    public async Task<IActionResult> Index(
        string? search,
        string? statusFilter,
        int page = 1)
    {
        int pageSize = 10;

        var query = _context.Majors
            .AsNoTracking()
            .Include(m => m.Faculty)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(m =>
                m.Code.Contains(search) ||
                m.Name.Contains(search) ||
                m.Faculty!.Name.Contains(search));
        }

        if (statusFilter == "active")
        {
            query = query.Where(m => m.IsActive);
        }
        else if (statusFilter == "inactive")
        {
            query = query.Where(m => !m.IsActive);
        }

        int totalCount = await query.CountAsync();

        var majors = await query
            .OrderBy(m => m.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MajorItemViewModel
            {
                Id = m.Id,
                Code = m.Code,
                Name = m.Name,
                FacultyName = m.Faculty != null
                    ? m.Faculty.Name
                    : "",
                IsActive = m.IsActive
            })
            .ToListAsync();

        var model = new MajorListViewModel
        {
            Majors = majors,
            Search = search,
            StatusFilter = statusFilter,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return View(model);
    }

    // 2. Thêm ngành - GET
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new MajorCreateEditViewModel();

        await LoadFaculties(model);

        return View(model);
    }

    // 3. Thêm ngành - POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MajorCreateEditViewModel model)
    {
        model.Code = model.Code?.Trim() ?? "";
        model.Name = model.Name?.Trim() ?? "";

        if (!ModelState.IsValid)
        {
            await LoadFaculties(model);
            return View(model);
        }

        var faculty = await _context.Faculties
            .FirstOrDefaultAsync(f => f.Id == model.FacultyId);

        if (faculty == null || !faculty.IsActive)
        {
            ModelState.AddModelError(
                nameof(model.FacultyId),
                "Vui lòng chọn khoa đang hoạt động.");

            await LoadFaculties(model);
            return View(model);
        }

        bool codeExists = await _context.Majors
            .AnyAsync(m => m.Code == model.Code);

        if (codeExists)
        {
            ModelState.AddModelError(
                nameof(model.Code),
                "Mã ngành đã tồn tại.");

            await LoadFaculties(model);
            return View(model);
        }

        var major = new Major
        {
            FacultyId = model.FacultyId,
            Code = model.Code,
            Name = model.Name,
            IsActive = true
        };

        _context.Majors.Add(major);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Thêm ngành thành công.";

        return RedirectToAction(nameof(Index));
    }

    // 4. Sửa ngành - GET
    [HttpGet]
    public async Task<IActionResult> Edit(ulong id)
    {
        var major = await _context.Majors
            .FirstOrDefaultAsync(m => m.Id == id);

        if (major == null)
            return NotFound();

        var model = new MajorCreateEditViewModel
        {
            Id = major.Id,
            FacultyId = major.FacultyId,
            Code = major.Code,
            Name = major.Name,
            IsActive = major.IsActive
        };

        await LoadFaculties(model, major.FacultyId);

        return View(model);
    }

    // 5. Sửa ngành - POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        MajorCreateEditViewModel model)
    {
        model.Code = model.Code?.Trim() ?? "";
        model.Name = model.Name?.Trim() ?? "";

        var major = await _context.Majors
            .FirstOrDefaultAsync(m => m.Id == model.Id);

        if (major == null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            await LoadFaculties(model, major.FacultyId);
            return View(model);
        }

        var faculty = await _context.Faculties
            .FirstOrDefaultAsync(f => f.Id == model.FacultyId);

        if (faculty == null ||
            (!faculty.IsActive && model.FacultyId != major.FacultyId))
        {
            ModelState.AddModelError(
                nameof(model.FacultyId),
                "Vui lòng chọn khoa đang hoạt động.");

            await LoadFaculties(model, major.FacultyId);
            return View(model);
        }

        bool codeExists = await _context.Majors
            .AnyAsync(m => m.Code == model.Code && m.Id != model.Id);

        if (codeExists)
        {
            ModelState.AddModelError(
                nameof(model.Code),
                "Mã ngành đã được sử dụng.");

            await LoadFaculties(model, major.FacultyId);
            return View(model);
        }

        major.FacultyId = model.FacultyId;
        major.Code = model.Code;
        major.Name = model.Name;
        major.IsActive = model.IsActive;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Cập nhật ngành thành công.";

        return RedirectToAction(nameof(Index));
    }

    // 6. Kích hoạt / vô hiệu hóa ngành
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(
        ulong id,
        string? search,
        string? statusFilter)
    {
        var major = await _context.Majors
            .FirstOrDefaultAsync(m => m.Id == id);

        if (major == null)
            return NotFound();

        major.IsActive = !major.IsActive;

        await _context.SaveChangesAsync();

        string status = major.IsActive
            ? "kích hoạt"
            : "vô hiệu hóa";

        TempData["SuccessMessage"] =
            $"Đã {status} ngành '{major.Name}' thành công.";

        return RedirectToAction(nameof(Index), new
        {
            search,
            statusFilter
        });
    }

    // 7. Nạp danh sách khoa vào dropdown
    private async Task LoadFaculties(
        MajorCreateEditViewModel model,
        ulong? currentFacultyId = null)
    {
        var query = _context.Faculties
            .AsNoTracking()
            .Where(f => f.IsActive);

        if (currentFacultyId.HasValue)
        {
            query = query.Where(f =>
                f.IsActive || f.Id == currentFacultyId.Value);
        }

        model.Faculties = await query
            .OrderBy(f => f.Code)
            .Select(f => new SelectListItem
            {
                Value = f.Id.ToString(),
                Text = f.Code + " - " + f.Name
            })
            .ToListAsync();
    }
}