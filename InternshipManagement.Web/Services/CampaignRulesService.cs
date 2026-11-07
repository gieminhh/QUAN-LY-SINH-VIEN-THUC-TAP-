using Microsoft.EntityFrameworkCore;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
namespace InternshipManagement.Web.Services;

public class CampaignRulesService(ApplicationDbContext db)
{
    public static readonly string[] Stages = { "REGISTRATION", "MATCHING", "SUBMISSION", "ASSESSMENT" };
    public static DateTime Now => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Asia/Ho_Chi_Minh");
    public async Task<string?> CheckWindow(ulong campaignId, string userId, string stage)
    {
        var campaign = await db.Campaigns.FindAsync(campaignId);
        if (campaign == null || campaign.Status != CampaignStatuses.Open) return "Đợt chưa mở hoặc đã đóng/lưu trữ.";
        var now = Now;
        if (await db.CampaignExceptions.AnyAsync(x => x.CampaignId == campaignId && x.UserId == userId && x.Stage == stage && x.ExpiresAt >= now)) return null;
        var window = await db.CampaignWindows.FirstOrDefaultAsync(x => x.CampaignId == campaignId && x.Stage == stage);
        if (window == null) return "Đợt chưa được cấu hình lịch cho thao tác này. Vui lòng liên hệ cán bộ quản lý.";
        if (now < window.OpensAt || now > window.ClosesAt) return "Thao tác nằm ngoài thời gian được phép của đợt.";
        return null;
    }
    public async Task<string?> Eligibility(ulong campaignId, StudentProfile student)
    {
        var policy = await db.CampaignEligibilityPolicies.FirstOrDefaultAsync(x => x.CampaignId == campaignId);
        if (policy == null) return "Chưa cấu hình điều kiện tham gia.";
        var evidence = await db.StudentEligibilityEvidence.FirstOrDefaultAsync(x => x.CampaignId == campaignId && x.StudentProfileId == student.Id);
        var gpa = await db.StudentGpaRecords.Include(x => x.Term).Where(x => x.StudentProfileId == student.Id).OrderByDescending(x => x.Term!.StartDate).FirstOrDefaultAsync();
        if (policy.MinimumGpa4.HasValue && (gpa?.Gpa4 == null || gpa.Gpa4 < policy.MinimumGpa4)) return "Thiếu hoặc chưa đạt GPA thang 4.";
        if (policy.MinimumGpa10.HasValue && (gpa?.Gpa10 == null || gpa.Gpa10 < policy.MinimumGpa10)) return "Thiếu hoặc chưa đạt GPA thang 10.";
        if (policy.MinimumCredits.HasValue && (evidence == null || evidence.Credits < policy.MinimumCredits)) return "Thiếu minh chứng hoặc chưa đủ tín chỉ.";
        if (!string.IsNullOrWhiteSpace(policy.PrerequisiteNote) && (evidence == null || !evidence.PrerequisitesMet)) return "Chưa xác nhận điều kiện tiên quyết.";
        return null;
    }
}
