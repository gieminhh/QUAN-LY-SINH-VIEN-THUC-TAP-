using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;

namespace InternshipManagement.Web.Controllers;

[Authorize]
public class DoanhNghiepController : Controller
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".pdf", ".doc", ".docx"];
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public DoanhNghiepController(
        ApplicationDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, TrangThaiHopTac? trangThai)
    {
        var query = _context.DoanhNghieps.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();
            query = query.Where(d =>
                d.Ten.Contains(keyword) ||
                d.MaSoThue.Contains(keyword) ||
                (d.LinhVuc != null && d.LinhVuc.Contains(keyword)));
        }

        if (trangThai.HasValue)
            query = query.Where(d => d.TrangThai == trangThai.Value);

        ViewBag.Search = search;
        ViewBag.TrangThai = trangThai;
        return View(await query.OrderBy(d => d.Ten).ToListAsync());
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Lecturer}")]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new DoanhNghiep());
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Lecturer}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DoanhNghiep doanhNghiep, IFormFile? fileHopTac)
    {
        doanhNghiep.Ten = doanhNghiep.Ten.Trim();
        doanhNghiep.MaSoThue = doanhNghiep.MaSoThue.Trim();

        if (await _context.DoanhNghieps.AnyAsync(d => d.MaSoThue == doanhNghiep.MaSoThue))
            ModelState.AddModelError(nameof(doanhNghiep.MaSoThue), "Mã số thuế này đã tồn tại.");

        ValidateUpload(fileHopTac);
        if (!ModelState.IsValid)
            return View(doanhNghiep);

        if (fileHopTac is not null)
            doanhNghiep.FileHopTac = await SaveUploadAsync(fileHopTac);

        _context.DoanhNghieps.Add(doanhNghiep);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Lecturer}")]
    [HttpGet]
    public async Task<IActionResult> Edit(ulong id)
    {
        var doanhNghiep = await _context.DoanhNghieps.FindAsync(id);
        return doanhNghiep is null ? NotFound() : View(doanhNghiep);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Lecturer}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(DoanhNghiep doanhNghiep, IFormFile? fileHopTac)
    {
        doanhNghiep.Ten = doanhNghiep.Ten.Trim();
        doanhNghiep.MaSoThue = doanhNghiep.MaSoThue.Trim();

        var existing = await _context.DoanhNghieps.FindAsync(doanhNghiep.Id);
        if (existing is null)
            return NotFound();

        if (await _context.DoanhNghieps.AnyAsync(d =>
                d.MaSoThue == doanhNghiep.MaSoThue && d.Id != doanhNghiep.Id))
            ModelState.AddModelError(nameof(doanhNghiep.MaSoThue), "Mã số thuế này đã được sử dụng.");

        ValidateUpload(fileHopTac);
        if (!ModelState.IsValid)
        {
            doanhNghiep.FileHopTac = existing.FileHopTac;
            return View(doanhNghiep);
        }

        existing.Ten = doanhNghiep.Ten;
        existing.MaSoThue = doanhNghiep.MaSoThue;
        existing.LinhVuc = doanhNghiep.LinhVuc;
        existing.DiaChi = doanhNghiep.DiaChi;
        existing.NguoiLienHe = doanhNghiep.NguoiLienHe;
        existing.SoDienThoai = doanhNghiep.SoDienThoai;
        existing.Email = doanhNghiep.Email;
        existing.TrangThai = doanhNghiep.TrangThai;

        if (fileHopTac is not null)
            existing.FileHopTac = await SaveUploadAsync(fileHopTac);

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(ulong id)
    {
        var doanhNghiep = await _context.DoanhNghieps.FindAsync(id);
        if (doanhNghiep is null)
            return NotFound();

        _context.DoanhNghieps.Remove(doanhNghiep);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private void ValidateUpload(IFormFile? file)
    {
        if (file is null)
            return;

        string extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            ModelState.AddModelError("fileHopTac", "Chỉ chấp nhận tệp .pdf, .doc hoặc .docx.");
        if (file.Length > MaxFileSize)
            ModelState.AddModelError("fileHopTac", "Tệp không được vượt quá 5 MB.");
    }

    private async Task<string> SaveUploadAsync(IFormFile file)
    {
        string directory = Path.Combine(_environment.WebRootPath, "uploads", "hoptac");
        Directory.CreateDirectory(directory);

        string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        string fullPath = Path.Combine(directory, fileName);
        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream);

        return $"/uploads/hoptac/{fileName}";
    }
}
