using System.Security.Claims;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Web.Controllers;

[Authorize]
public class UngTuyenController : Controller
{
    private const long MaxCvSize = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedCvExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx"
    };

    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public UngTuyenController(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [Authorize(Roles = AppRoles.Student)]
    public async Task<IActionResult> Index(string? tuKhoa, ulong? doanhNghiepId)
    {
        var query = _context.InternshipPositions
            .AsNoTracking()
            .Where(p => p.IsOpen
                && p.ApprovalStatus == InternshipPositionApprovalStatuses.Approved
                && p.CreatedByUser != null
                && p.CreatedByUser.DoanhNghiepId != null);

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var keyword = tuKhoa.Trim().ToLower();
            query = query.Where(p => p.Title.ToLower().Contains(keyword)
                || p.Description.ToLower().Contains(keyword)
                || (p.CreatedByUser!.DoanhNghiep != null
                    && p.CreatedByUser.DoanhNghiep.Ten.ToLower().Contains(keyword)));
        }

        if (doanhNghiepId.HasValue)
        {
            query = query.Where(p => p.CreatedByUser!.DoanhNghiepId == doanhNghiepId.Value);
        }

        var positions = await query
            .Include(p => p.CreatedByUser)
            .ThenInclude(u => u!.DoanhNghiep)
            .OrderBy(p => p.Title)
            .ToListAsync();

        ViewBag.DoanhNghieps = await _context.DoanhNghieps
            .AsNoTracking()
            .Where(d => _context.InternshipPositions.Any(p =>
                p.IsOpen
                && p.ApprovalStatus == InternshipPositionApprovalStatuses.Approved
                && p.CreatedByUser != null
                && p.CreatedByUser.DoanhNghiepId == d.Id))
            .OrderBy(d => d.Ten)
            .ToListAsync();
        ViewBag.TuKhoa = tuKhoa;
        ViewBag.DoanhNghiepId = doanhNghiepId;
        return View(positions);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<IActionResult> Apply(ulong viTriThucTapId, IFormFile? fileCV)
    {
        var position = await _context.InternshipPositions
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == viTriThucTapId);
        if (position == null)
        {
            return NotFound();
        }

        if (!position.IsOpen || position.ApprovalStatus != InternshipPositionApprovalStatuses.Approved)
        {
            TempData["ErrorMessage"] = "Vị trí thực tập này đã đóng hoặc chưa được duyệt.";
            return RedirectToAction(nameof(Index));
        }

        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (await _context.HoSoUngTuyens.AnyAsync(h =>
            h.ViTriThucTapId == viTriThucTapId && h.SinhVienId == studentId))
        {
            TempData["ErrorMessage"] = "Bạn đã nộp hồ sơ cho vị trí này rồi.";
            return RedirectToAction(nameof(HoSoCuaToi));
        }

        string? storedFilePath = null;
        string? storedFileName = null;
        if (fileCV is { Length: > 0 })
        {
            var extension = Path.GetExtension(fileCV.FileName);
            if (fileCV.Length > MaxCvSize)
            {
                TempData["ErrorMessage"] = "File CV không được vượt quá 5MB.";
                return RedirectToAction(nameof(Index));
            }

            if (!AllowedCvExtensions.Contains(extension))
            {
                TempData["ErrorMessage"] = "File CV chỉ chấp nhận định dạng PDF, DOC hoặc DOCX.";
                return RedirectToAction(nameof(Index));
            }

            storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var uploadDirectory = Path.Combine(_environment.WebRootPath, "uploads", "cv");
            Directory.CreateDirectory(uploadDirectory);
            storedFilePath = Path.Combine(uploadDirectory, storedFileName);
            await using var stream = new FileStream(storedFilePath, FileMode.CreateNew);
            await fileCV.CopyToAsync(stream);
        }
        _context.HoSoUngTuyens.Add(new HoSoUngTuyen
        {
            ViTriThucTapId = viTriThucTapId,
            SinhVienId = studentId,
            NgayNop = DateTime.UtcNow,
            TrangThai = TrangThaiUngTuyen.DaNop,
            FileCV = storedFileName == null ? null : $"/uploads/cv/{storedFileName}"
        });

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            if (await _context.HoSoUngTuyens.AnyAsync(h =>
                h.ViTriThucTapId == viTriThucTapId && h.SinhVienId == studentId))
            {
                if (storedFilePath != null && System.IO.File.Exists(storedFilePath))
                {
                    System.IO.File.Delete(storedFilePath);
                }

                TempData["ErrorMessage"] = "Bạn đã nộp hồ sơ cho vị trí này rồi.";
                return RedirectToAction(nameof(HoSoCuaToi));
            }

            throw;
        }

        TempData["SuccessMessage"] = "Đã nộp hồ sơ ứng tuyển.";
        return RedirectToAction(nameof(HoSoCuaToi));
    }

    [Authorize(Roles = AppRoles.Student)]
    public async Task<IActionResult> HoSoCuaToi()
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var applications = await _context.HoSoUngTuyens
            .AsNoTracking()
            .Where(h => h.SinhVienId == studentId)
            .Include(h => h.ViTriThucTap)
            .ThenInclude(p => p!.CreatedByUser)
            .ThenInclude(u => u!.DoanhNghiep)
            .OrderByDescending(h => h.NgayNop)
            .ToListAsync();

        return View(applications);
    }

    [Authorize(Roles = AppRoles.Company)]
    public async Task<IActionResult> DanhSachHoSo()
    {
        var companyId = await GetCurrentCompanyIdAsync();
        if (!companyId.HasValue)
        {
            return Forbid();
        }

        var applications = await _context.HoSoUngTuyens
            .AsNoTracking()
            .Where(h => h.ViTriThucTap != null
                && h.ViTriThucTap.CreatedByUser != null
                && h.ViTriThucTap.CreatedByUser.DoanhNghiepId == companyId.Value)
            .Include(h => h.ViTriThucTap)
            .Include(h => h.SinhVien)
            .OrderByDescending(h => h.NgayNop)
            .ToListAsync();

        return View(applications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Company)]
    public async Task<IActionResult> PhanHoi(ulong id, TrangThaiUngTuyen trangThai, string? ghiChuPhanHoi)
    {
        var application = await _context.HoSoUngTuyens
            .Include(h => h.ViTriThucTap)
            .ThenInclude(p => p!.CreatedByUser)
            .SingleOrDefaultAsync(h => h.Id == id);
        if (application == null)
        {
            return NotFound();
        }

        var companyId = await GetCurrentCompanyIdAsync();
        if (!companyId.HasValue
            || application.ViTriThucTap?.CreatedByUser?.DoanhNghiepId != companyId.Value)
        {
            return Forbid();
        }

        if (application.TrangThai is TrangThaiUngTuyen.ChapNhan or TrangThaiUngTuyen.TuChoi)
        {
            TempData["ErrorMessage"] = "Hồ sơ đã được chấp nhận hoặc từ chối, không thể đổi trạng thái.";
            return RedirectToAction(nameof(DanhSachHoSo));
        }

        if (trangThai is not (TrangThaiUngTuyen.DangPhongVan
            or TrangThaiUngTuyen.ChapNhan
            or TrangThaiUngTuyen.TuChoi))
        {
            TempData["ErrorMessage"] = "Trạng thái phản hồi không hợp lệ.";
            return RedirectToAction(nameof(DanhSachHoSo));
        }

        application.TrangThai = trangThai;
        application.GhiChuPhanHoi = string.IsNullOrWhiteSpace(ghiChuPhanHoi)
            ? null
            : ghiChuPhanHoi.Trim();
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã cập nhật phản hồi hồ sơ.";
        return RedirectToAction(nameof(DanhSachHoSo));
    }

    private async Task<ulong?> GetCurrentCompanyIdAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.DoanhNghiepId)
            .SingleOrDefaultAsync();
    }
}
