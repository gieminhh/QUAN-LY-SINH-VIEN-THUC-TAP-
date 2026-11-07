using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.ViewModels.Faculties;

namespace InternshipManagement.Web.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.TrainingManager}")]
public class FacultiesController : Controller
{
    private readonly ApplicationDbContext _context;

    public FacultiesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // 1. Danh sách khoa
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? statusFilter,
        int page = 1)
    {
        int pageSize = 10;

        if (page < 1)
            page = 1;

        var query = _context.Faculties
            .AsNoTracking()
            .AsQueryable();

        // Tìm kiếm theo mã hoặc tên khoa
        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();

            query = query.Where(f =>
                f.Code.Contains(keyword) ||
                f.Name.Contains(keyword));
        }

        // Lọc trạng thái
        if (statusFilter == "active")
        {
            query = query.Where(f => f.IsActive);
        }
        else if (statusFilter == "inactive")
        {
            query = query.Where(f => !f.IsActive);
        }

        int totalCount = await query.CountAsync();

        var faculties = await query
            .OrderBy(f => f.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FacultyItemViewModel
            {
                Id = f.Id,
                Code = f.Code,
                Name = f.Name,
                IsActive = f.IsActive,
                MajorCount = f.Majors.Count()
            })
            .ToListAsync();

        var model = new FacultyListViewModel
        {
            Faculties = faculties,
            Search = search,
            StatusFilter = statusFilter,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return View(model);
    }

    // 2. Hiển thị form thêm khoa
    [HttpGet]
    public IActionResult Create()
    {
        var model = new FacultyCreateEditViewModel
        {
            IsActive = true
        };

        return View(model);
    }

    // 3. Xử lý thêm khoa
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FacultyCreateEditViewModel model)
    {
        model.Code = model.Code?.Trim() ?? string.Empty;
        model.Name = model.Name?.Trim() ?? string.Empty;

        if (!ModelState.IsValid)
            return View(model);

        bool duplicateCode = await _context.Faculties
            .AnyAsync(f => f.Code == model.Code);

        if (duplicateCode)
        {
            ModelState.AddModelError(
                nameof(model.Code),
                "Mã khoa này đã tồn tại.");

            return View(model);
        }

        var faculty = new Faculty
        {
            Code = model.Code,
            Name = model.Name,
            IsActive = model.IsActive
        };

        _context.Faculties.Add(faculty);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"Thêm khoa '{faculty.Name}' thành công!";

        return RedirectToAction(nameof(Index));
    }

    // 4. Hiển thị form chỉnh sửa
    [HttpGet]
    public async Task<IActionResult> Edit(ulong id)
    {
        var faculty = await _context.Faculties
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id);

        if (faculty == null)
            return NotFound();

        var model = new FacultyCreateEditViewModel
        {
            Id = faculty.Id,
            Code = faculty.Code,
            Name = faculty.Name,
            IsActive = faculty.IsActive
        };

        return View(model);
    }

    // 5. Xử lý chỉnh sửa
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(FacultyCreateEditViewModel model)
    {
        model.Code = model.Code?.Trim() ?? string.Empty;
        model.Name = model.Name?.Trim() ?? string.Empty;

        if (!ModelState.IsValid)
            return View(model);

        var faculty = await _context.Faculties
            .FirstOrDefaultAsync(f => f.Id == model.Id);

        if (faculty == null)
            return NotFound();

        bool duplicateCode = await _context.Faculties
            .AnyAsync(f => f.Code == model.Code && f.Id != model.Id);

        if (duplicateCode)
        {
            ModelState.AddModelError(
                nameof(model.Code),
                "Mã khoa này đã được sử dụng.");

            return View(model);
        }

        faculty.Code = model.Code;
        faculty.Name = model.Name;
        faculty.IsActive = model.IsActive;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"Cập nhật khoa '{faculty.Name}' thành công!";

        return RedirectToAction(nameof(Index));
    }

    // 6. Bật / tắt trạng thái khoa
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(
        ulong id,
        string? search,
        string? statusFilter)
    {
        var faculty = await _context.Faculties
            .FirstOrDefaultAsync(f => f.Id == id);

        if (faculty == null)
            return NotFound();

        faculty.IsActive = !faculty.IsActive;

        await _context.SaveChangesAsync();

        string status = faculty.IsActive
            ? "kích hoạt"
            : "vô hiệu hóa";

        TempData["SuccessMessage"] =
            $"Đã {status} khoa '{faculty.Name}' thành công.";

        return RedirectToAction(nameof(Index), new
        {
            search,
            statusFilter
        });
    }
}