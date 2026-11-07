using System.Data;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;

namespace InternshipManagement.Web.Services;

// Both management screens use the same assignment count, eligibility checks and transaction.
public class StudentAssignmentService(ApplicationDbContext db, IUserScopeService scopes, CampaignRulesService rules)
{
    public async Task<(bool Success, string Message)> AssignAsync(string actor, ulong campaignId,
        ulong studentId, ulong lecturerId, ulong? mentorId, string? reason, bool requestException = false)
    {
        if (!await scopes.CanManageCampaignAsync(actor, campaignId)) return (false, "Bạn không có quyền quản lý đợt này.");
        var windowError = await rules.CheckWindow(campaignId, actor, "MATCHING");
        if (windowError != null) return (false, windowError);
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var error = await ValidateAsync(campaignId, studentId, lecturerId, mentorId);
            if (error != null) return (false, error);
            var student = (await db.StudentProfiles.FindAsync(studentId))!;
            var lecturer = (await db.LecturerProfiles.FindAsync(lecturerId))!;
            if (await db.LecturerCampaignAssignments.AnyAsync(a => a.CampaignId == campaignId &&
                a.StudentUserId == student.UserId && a.Status == LecturerAssignmentStatuses.PendingException))
                return (false, "Sinh viên đang có yêu cầu ngoại lệ chờ duyệt trong đợt.");
            var quota = await db.LecturerCampaignQuotas.FirstOrDefaultAsync(q => q.CampaignId == campaignId && q.LecturerProfileId == lecturerId);
            var count = await db.StudentAssignments.CountAsync(a => a.CampaignId == campaignId && a.LecturerProfileId == lecturerId);
            var overQuota = count >= (quota?.MaxStudents ?? 0);
            if (reason?.Length > 1000) return (false, "Lý do ngoại lệ tối đa 1000 ký tự.");
            if (overQuota && string.IsNullOrWhiteSpace(reason)) return (false, "Đã hết hạn mức hướng dẫn. Cần phê duyệt ngoại lệ với lý do cụ thể.");
            if (requestException && quota == null) return (false, "Giảng viên chưa được thiết lập quota cho đợt này.");
            var pending = overQuota && requestException;
            db.LecturerCampaignAssignments.Add(new()
            {
                CampaignId = campaignId, StudentUserId = student.UserId, LecturerUserId = lecturer.UserId,
                CompanyMentorId = mentorId,
                Status = pending ? LecturerAssignmentStatuses.PendingException : overQuota ? LecturerAssignmentStatuses.ApprovedException : LecturerAssignmentStatuses.Assigned,
                ExceptionReason = overQuota ? reason!.Trim() : null,
                ReviewedByUserId = overQuota && !pending ? actor : null,
                ReviewedAt = overQuota && !pending ? DateTime.UtcNow : null
            });
            if (!pending) AddAssignment(actor, campaignId, studentId, lecturerId, mentorId, overQuota ? reason : null);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return (true, pending ? "Đã gửi yêu cầu vượt quota để chờ duyệt; sinh viên chưa được tính là đã phân công." : "Đã phân công và lưu người phê duyệt.");
        }
        catch (Exception e) when (e.GetBaseException() is MySqlException { Number: 1213 or 1205 or 1062 })
        {
            db.ChangeTracker.Clear();
            return (false, "Có yêu cầu phân công đồng thời hoặc dữ liệu vừa thay đổi. Vui lòng tải lại danh sách và thực hiện lại.");
        }
    }

    public async Task<(bool Success, string Message)> ReviewAsync(string actor, ulong id, bool approve)
    {
        var snapshot = await db.LecturerCampaignAssignments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
        if (snapshot == null || !await scopes.CanManageCampaignAsync(actor, snapshot.CampaignId)) return (false, "Không có quyền xử lý yêu cầu này.");
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var request = await db.LecturerCampaignAssignments.FirstAsync(a => a.Id == id);
            if (request.Status != LecturerAssignmentStatuses.PendingException) return (false, "Yêu cầu ngoại lệ đã được xử lý.");
            if (approve)
            {
                var student = await db.StudentProfiles.FirstOrDefaultAsync(s => s.UserId == request.StudentUserId);
                var lecturer = await db.LecturerProfiles.FirstOrDefaultAsync(l => l.UserId == request.LecturerUserId);
                if (student == null || lecturer == null) return (false, "Không tìm thấy hồ sơ sinh viên hoặc giảng viên.");
                var error = await rules.CheckWindow(request.CampaignId, actor, "MATCHING") ??
                    await ValidateAsync(request.CampaignId, student.Id, lecturer.Id, request.CompanyMentorId);
                if (error != null) return (false, error);
                AddAssignment(actor, request.CampaignId, student.Id, lecturer.Id, request.CompanyMentorId, request.ExceptionReason);
            }
            request.Status = approve ? LecturerAssignmentStatuses.ApprovedException : LecturerAssignmentStatuses.RejectedException;
            request.ReviewedByUserId = actor;
            request.ReviewedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return (true, approve ? "Đã duyệt ngoại lệ và phân công sinh viên." : "Đã từ chối yêu cầu ngoại lệ.");
        }
        catch (Exception e) when (e.GetBaseException() is MySqlException { Number: 1213 or 1205 or 1062 })
        {
            db.ChangeTracker.Clear();
            return (false, "Yêu cầu vừa được xử lý đồng thời. Vui lòng tải lại danh sách.");
        }
    }

    async Task<string?> ValidateAsync(ulong campaignId, ulong studentId, ulong lecturerId, ulong? mentorId)
    {
        var campaign = await db.Campaigns.Include(c => c.Major).FirstOrDefaultAsync(c => c.Id == campaignId);
        var student = await db.StudentProfiles.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == studentId);
        var lecturer = await db.LecturerProfiles.Include(l => l.User).FirstOrDefaultAsync(l => l.Id == lecturerId);
        if (campaign == null || student == null || lecturer == null || student.MajorId != campaign.MajorId || lecturer.FacultyId != campaign.Major!.FacultyId)
            return "Sinh viên, giảng viên không thuộc ngành/khoa của đợt.";
        if (student.User?.IsActive != true || lecturer.User?.IsActive != true ||
            !await HasRole(student.UserId, AppRoles.Student) || !await HasRole(lecturer.UserId, AppRoles.Lecturer))
            return "Tài khoản sinh viên/giảng viên không hoạt động hoặc không còn vai trò phù hợp.";
        if (await db.StudentAssignments.AnyAsync(a => a.CampaignId == campaignId && a.StudentProfileId == studentId)) return "Sinh viên đã có phân công trong đợt.";
        var error = await rules.Eligibility(campaignId, student);
        if (error != null) return error;
        if (mentorId.HasValue && !await db.CompanyMentors.AnyAsync(m => m.Id == mentorId && db.Companies.Any(c => c.Id == m.CompanyId && c.Status == "APPROVED" && c.MajorId == campaign.MajorId)))
            return "Người hướng dẫn doanh nghiệp chưa được xác minh cho ngành này.";
        if (campaign.Kind == CampaignKinds.Internship && !mentorId.HasValue) return "Đợt thực tập cần người hướng dẫn doanh nghiệp.";
        return null;
    }
    Task<bool> HasRole(string userId, string role) => db.UserRoles.AnyAsync(ur => ur.UserId == userId && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == role));
    void AddAssignment(string actor, ulong campaign, ulong student, ulong lecturer, ulong? mentor, string? reason) => db.StudentAssignments.Add(new()
    {
        CampaignId = campaign, StudentProfileId = student, LecturerProfileId = lecturer, CompanyMentorId = mentor,
        QuotaOverrideReason = reason?.Trim(), AssignedBy = actor
    });
}
