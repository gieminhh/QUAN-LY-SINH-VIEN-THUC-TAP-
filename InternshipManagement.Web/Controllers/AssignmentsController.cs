using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using System.Security.Claims;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels;
namespace InternshipManagement.Web.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.TrainingManager},{AppRoles.FacultyManager}")]
public class AssignmentsController(ApplicationDbContext db, IUserScopeService scopes, CampaignRulesService rules) : Controller
{
    string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpGet]
    public async Task<IActionResult> Index(ulong? campaignId)
    {
        var query = await scopes.FilterAccessibleCampaignsAsync(Actor, db.Campaigns); var campaigns = await query.Include(c => c.Major).OrderByDescending(c => c.CreatedAt).ToListAsync();
        if (campaigns.Count == 0) { TempData["ErrorMessage"] = "Chưa có đợt được phân quyền."; return RedirectToAction("Index", "Home"); }
        var campaign = campaigns.FirstOrDefault(c => c.Id == (campaignId ?? campaigns[0].Id)); if (campaign == null) return Forbid();
        return View(new AssignmentViewModel { Campaign = campaign, Campaigns = campaigns, Students = await db.StudentProfiles.Include(s => s.User).Where(s => s.MajorId == campaign.MajorId).ToListAsync(), Lecturers = await db.LecturerProfiles.Include(l => l.User).Where(l => l.FacultyId == campaign.Major!.FacultyId).ToListAsync(), Mentors = await db.CompanyMentors.Where(m => db.Companies.Any(c => c.Id == m.CompanyId && c.Status == "APPROVED" && c.MajorId == campaign.MajorId)).ToListAsync(), Assignments = await db.StudentAssignments.Where(a => a.CampaignId == campaign.Id).ToListAsync() });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(ulong campaignId, ulong studentId, ulong lecturerId, ulong? mentorId, string? quotaOverrideReason)
    {
        if (!await scopes.CanManageCampaignAsync(Actor, campaignId)) return Forbid();
        var windowError = await rules.CheckWindow(campaignId, Actor, "MATCHING"); if (windowError != null) { TempData["ErrorMessage"] = windowError; return RedirectToAction(nameof(Index), new { campaignId }); }
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var campaign = await db.Campaigns.Include(c => c.Major).FirstOrDefaultAsync(c => c.Id == campaignId); var student = await db.StudentProfiles.FindAsync(studentId); var lecturer = await db.LecturerProfiles.FindAsync(lecturerId);
            string? error = null;
            if (campaign == null || student == null || lecturer == null || student.MajorId != campaign.MajorId || lecturer.FacultyId != campaign.Major!.FacultyId) error = "Sinh viên, giảng viên không thuộc ngành/khoa của đợt.";
            else if (await db.StudentAssignments.AnyAsync(a => a.CampaignId == campaignId && a.StudentProfileId == studentId)) error = "Sinh viên đã có phân công trong đợt.";
            else error = await rules.Eligibility(campaignId, student);
            if (error == null && mentorId.HasValue && !await db.CompanyMentors.AnyAsync(m => m.Id == mentorId && db.Companies.Any(c => c.Id == m.CompanyId && c.Status == "APPROVED" && c.MajorId == campaign!.MajorId))) error = "Người hướng dẫn doanh nghiệp chưa được xác minh cho ngành này.";
            if (error == null && campaign!.Kind == CampaignKinds.Internship && !mentorId.HasValue) error = "Đợt thực tập cần người hướng dẫn doanh nghiệp.";
            var quota = await db.LecturerCampaignQuotas.FirstOrDefaultAsync(q => q.CampaignId == campaignId && q.LecturerProfileId == lecturerId);
            var count = await db.StudentAssignments.CountAsync(a => a.CampaignId == campaignId && a.LecturerProfileId == lecturerId);
            if (error == null && count >= (quota?.MaxStudents ?? 0) && string.IsNullOrWhiteSpace(quotaOverrideReason)) error = "Đã hết hạn mức hướng dẫn. Cần phê duyệt ngoại lệ với lý do cụ thể.";
            if (quotaOverrideReason?.Length > 1000) error = "Lý do ngoại lệ tối đa 1000 ký tự.";
            if (error != null) { await tx.RollbackAsync(); TempData["ErrorMessage"] = error; return RedirectToAction(nameof(Index), new { campaignId }); }
            db.StudentAssignments.Add(new() { CampaignId = campaignId, StudentProfileId = studentId, LecturerProfileId = lecturerId, CompanyMentorId = mentorId, QuotaOverrideReason = quotaOverrideReason?.Trim(), AssignedBy = Actor }); await db.SaveChangesAsync(); await tx.CommitAsync(); TempData["SuccessMessage"] = "Đã phân công và lưu người phê duyệt."; return RedirectToAction(nameof(Index), new { campaignId });
        }
        catch (Exception e) when (e.GetBaseException() is MySqlException { Number: 1213 or 1205 or 1062 })
        {
            db.ChangeTracker.Clear();
            TempData["ErrorMessage"] = "Có yêu cầu phân công đồng thời hoặc dữ liệu vừa thay đổi. Vui lòng tải lại danh sách và thực hiện lại.";
            return RedirectToAction(nameof(Index), new { campaignId });
        }
    }
}
