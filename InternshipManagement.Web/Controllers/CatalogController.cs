using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.ViewModels;
namespace InternshipManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class CatalogController(ApplicationDbContext db) : Controller
{
    async Task Fill(CatalogViewModel m)
    {
        m.Rows = m.Kind switch
        {
            "faculty" => await db.Faculties.Select(x => new CatalogRow(x.Id, x.Code, x.Name)).ToListAsync(),
            "major" => await db.Majors.Include(x => x.Faculty).Select(x => new CatalogRow(x.Id, x.Code, x.Name + " — " + x.Faculty!.Name)).ToListAsync(),
            "year" => await db.AcademicYears.Select(x => new CatalogRow(x.Id, x.Code, x.StartDate.ToString() + " — " + x.EndDate.ToString())).ToListAsync(),
            "term" => await db.Terms.Include(x => x.AcademicYear).Select(x => new CatalogRow(x.Id, "HK" + x.TermNo, x.AcademicYear!.Code)).ToListAsync(),
            _ => new()
        };
        if (m.Kind == "major") m.Parents = await db.Faculties.Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToListAsync();
        if (m.Kind == "term") m.Parents = await db.AcademicYears.Select(x => new SelectListItem(x.Code, x.Id.ToString())).ToListAsync();
    }
    [HttpGet]
    public async Task<IActionResult> Index(string kind = "faculty", ulong id = 0)
    {
        if (!new[] { "faculty", "major", "year", "term" }.Contains(kind)) return BadRequest();
        var m = new CatalogViewModel { Kind = kind, Id = id };
        if (id > 0) switch (kind)
            {
                case "faculty": var f = await db.Faculties.FindAsync(id); if (f == null) return NotFound(); m.Code = f.Code; m.Name = f.Name; m.IsActive = f.IsActive; break;
                case "major": var a = await db.Majors.FindAsync(id); if (a == null) return NotFound(); m.Code = a.Code; m.Name = a.Name; m.IsActive = a.IsActive; m.ParentId = a.FacultyId; break;
                case "year": var y = await db.AcademicYears.FindAsync(id); if (y == null) return NotFound(); m.Code = y.Code; m.StartDate = y.StartDate; m.EndDate = y.EndDate; break;
                case "term": var t = await db.Terms.FindAsync(id); if (t == null) return NotFound(); m.ParentId = t.AcademicYearId; m.TermNo = t.TermNo; m.StartDate = t.StartDate; m.EndDate = t.EndDate; break;
            }
        await Fill(m); return View(m);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(CatalogViewModel m)
    {
        if (!new[] { "faculty", "major", "year", "term" }.Contains(m.Kind)) return BadRequest();
        if (m.Kind == "term") ModelState.Remove(nameof(m.Code));
        if (m.Kind is "faculty" or "major" && string.IsNullOrWhiteSpace(m.Name)) ModelState.AddModelError("Name", "Vui lòng nhập tên.");
        if (m.Kind is "year" or "term" && m.EndDate < m.StartDate) ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu.");
        if (m.Kind == "major" && !await db.Faculties.AnyAsync(x => x.Id == m.ParentId)) ModelState.AddModelError("ParentId", "Khoa không tồn tại.");
        if (m.Kind == "term")
        {
            var year = await db.AcademicYears.FindAsync(m.ParentId);
            if (year == null || m.TermNo < 1 || m.TermNo > 3 || m.StartDate < year.StartDate || m.EndDate > year.EndDate) ModelState.AddModelError("", "Học kỳ phải thuộc năm học đã chọn, số kỳ từ 1 đến 3.");
        }
        if (m.Kind == "year" && m.Id > 0 && await db.Terms.AnyAsync(t => t.AcademicYearId == m.Id && (t.StartDate < m.StartDate || t.EndDate > m.EndDate)))
            ModelState.AddModelError("", "Khoảng ngày năm học phải bao phủ tất cả học kỳ hiện có.");
        if (!ModelState.IsValid) { await Fill(m); return View("Index", m); }
        switch (m.Kind)
        {
            case "faculty": var f = m.Id == 0 ? new Faculty() : await db.Faculties.FindAsync(m.Id); if (f == null) return NotFound(); f.Code = m.Code.Trim(); f.Name = m.Name!.Trim(); f.IsActive = m.IsActive; if (m.Id == 0) db.Faculties.Add(f); break;
            case "major": var a = m.Id == 0 ? new Major() : await db.Majors.FindAsync(m.Id); if (a == null) return NotFound(); a.Code = m.Code.Trim(); a.Name = m.Name!.Trim(); a.FacultyId = m.ParentId; a.IsActive = m.IsActive; if (m.Id == 0) db.Majors.Add(a); break;
            case "year": var y = m.Id == 0 ? new AcademicYear() : await db.AcademicYears.FindAsync(m.Id); if (y == null) return NotFound(); y.Code = m.Code.Trim(); y.StartDate = m.StartDate; y.EndDate = m.EndDate; if (m.Id == 0) db.AcademicYears.Add(y); break;
            case "term": var t = m.Id == 0 ? new Term() : await db.Terms.FindAsync(m.Id); if (t == null) return NotFound(); t.AcademicYearId = m.ParentId; t.TermNo = m.TermNo; t.StartDate = m.StartDate; t.EndDate = m.EndDate; if (m.Id == 0) db.Terms.Add(t); break;
        }
        try { await db.SaveChangesAsync(); } catch (DbUpdateException) { ModelState.AddModelError("", "Mã/số học kỳ trùng hoặc dữ liệu đang được sử dụng."); await Fill(m); return View("Index", m); }
        TempData["SuccessMessage"] = "Đã lưu danh mục."; return RedirectToAction(nameof(Index), new { kind = m.Kind });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string kind, ulong id)
    {
        object? entity = kind switch { "faculty" => await db.Faculties.FindAsync(id), "major" => await db.Majors.FindAsync(id), "year" => await db.AcademicYears.FindAsync(id), "term" => await db.Terms.FindAsync(id), _ => null };
        if (entity == null) return NotFound(); db.Remove(entity);
        try { await db.SaveChangesAsync(); TempData["SuccessMessage"] = "Đã xóa bản ghi chưa được sử dụng."; } catch (DbUpdateException) { TempData["ErrorMessage"] = "Không thể xóa danh mục đang có dữ liệu liên quan. Hãy ngừng sử dụng thay vì xóa."; }
        return RedirectToAction(nameof(Index), new { kind });
    }
}
