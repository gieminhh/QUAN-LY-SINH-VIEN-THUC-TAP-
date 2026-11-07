using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.ViewModels;

namespace InternshipManagement.Web.Services;

public class LecturerProfileService : ILecturerProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserScopeService _scopeService;

    public LecturerProfileService(ApplicationDbContext context, IUserScopeService scopeService)
    {
        _context = context;
        _scopeService = scopeService;
    }

    public async Task<LecturerProfile?> GetProfileByUserIdAsync(string userId)
    {
        return await _context.LecturerProfiles
            .Include(p => p.User)
            .Include(p => p.Faculty)
            .Include(p => p.Quotas)
                .ThenInclude(q => q.Campaign)
            .Include(p => p.Topics)
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<LecturerProfile?> GetProfileByIdAsync(ulong profileId)
    {
        return await _context.LecturerProfiles
            .Include(p => p.User)
            .Include(p => p.Faculty)
            .Include(p => p.Quotas)
                .ThenInclude(q => q.Campaign)
            .Include(p => p.Topics)
            .FirstOrDefaultAsync(p => p.Id == profileId);
    }

    public async Task<LecturerProfileViewModel?> GetProfileViewModelByUserIdAsync(string userId)
    {
        var profile = await GetProfileByUserIdAsync(userId);
        if (profile == null) return null;

        return new LecturerProfileViewModel
        {
            Id = profile.Id,
            UserId = profile.UserId,
            FullName = profile.User?.FullName ?? string.Empty,
            LecturerCode = profile.LecturerCode,
            Email = profile.User?.Email ?? string.Empty,
            FacultyName = profile.Faculty?.Name ?? "Chưa phân khoa",
            DepartmentName = profile.DepartmentName,
            AcademicDegree = profile.AcademicDegree,
            AcademicTitle = profile.AcademicTitle,
            Expertise = profile.Expertise,
            ResearchInterests = profile.ResearchInterests,
            Quotas = profile.Quotas.OrderByDescending(q => q.UpdatedAt).ToList(),
            Topics = profile.Topics.OrderByDescending(t => t.CreatedAt).ToList()
        };
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateProfileAsync(string userId, LecturerProfileEditViewModel model)
    {
        var profile = await _context.LecturerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            return (false, "Không tìm thấy hồ sơ giảng viên tương ứng.");
        }

        // Only update allowed fields (department_name, academic_degree, academic_title, expertise, research_interests)
        // LecturerCode, FacultyId, and Quotas CANNOT be changed by lecturer
        profile.DepartmentName = model.DepartmentName?.Trim();
        profile.AcademicDegree = model.AcademicDegree?.Trim();
        profile.AcademicTitle = model.AcademicTitle?.Trim();
        profile.Expertise = model.Expertise?.Trim();
        profile.ResearchInterests = model.ResearchInterests?.Trim();
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<LecturerQuotaListViewModel> GetQuotaListViewModelAsync(ulong campaignId, string? managerUserId = null)
    {
        var campaign = await _context.Campaigns
            .Include(c => c.Major)
                .ThenInclude(m => m!.Faculty)
            .Include(c => c.Term)
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        var viewModel = new LecturerQuotaListViewModel
        {
            CampaignId = campaignId,
            Campaign = campaign
        };

        if (campaign == null) return viewModel;

        // Find lecturers belonging to this campaign's faculty
        var facultyId = campaign.Major?.FacultyId ?? 0;
        var lecturersQuery = _context.LecturerProfiles
            .Include(p => p.User)
            .Include(p => p.Faculty)
            .Where(p => facultyId == 0 || p.FacultyId == facultyId);

        var lecturers = await lecturersQuery.OrderBy(p => p.LecturerCode).ToListAsync();

        var quotas = await _context.LecturerCampaignQuotas
            .Where(q => q.CampaignId == campaignId)
            .ToListAsync();

        var topics = await _context.Topics
            .Where(t => t.CampaignId == campaignId)
            .ToListAsync();

        foreach (var l in lecturers)
        {
            var quota = quotas.FirstOrDefault(q => q.LecturerProfileId == l.Id);
            var lecturerTopics = topics.Where(t => t.LecturerProfileId == l.Id).ToList();

            viewModel.Lecturers.Add(new LecturerQuotaItemViewModel
            {
                LecturerProfileId = l.Id,
                LecturerCode = l.LecturerCode,
                FullName = l.User?.FullName ?? l.LecturerCode,
                FacultyName = l.Faculty?.Name ?? string.Empty,
                DepartmentName = l.DepartmentName,
                AcademicDegree = l.AcademicDegree,
                MaxStudents = quota?.MaxStudents ?? 0,
                TotalRegisteredTopics = lecturerTopics.Count,
                ApprovedTopics = lecturerTopics.Count(t => t.Status == TopicStatuses.Approved)
            });
        }

        return viewModel;
    }

    public async Task<(bool Success, string? ErrorMessage)> SetQuotaAsync(ulong lecturerProfileId, ulong campaignId, uint maxStudents, string assignedByUserId)
    {
        var lecturer = await _context.LecturerProfiles.FindAsync(lecturerProfileId);
        if (lecturer == null) return (false, "Không tìm thấy giảng viên.");

        var campaign = await _context.Campaigns.FindAsync(campaignId);
        if (campaign == null) return (false, "Không tìm thấy đợt thực tập / đồ án.");

        var campaignMajor=await _context.Majors.FindAsync(campaign.MajorId);
        if(campaignMajor==null||campaignMajor.FacultyId!=lecturer.FacultyId)return (false,"Giảng viên không thuộc khoa của đợt.");
        if(maxStudents>1000)return (false,"Hạn mức không hợp lệ.");
        // Check if assignedByUserId has permission over campaign
        if (!string.IsNullOrEmpty(assignedByUserId))
        {
            var canManage = await _scopeService.CanManageCampaignAsync(assignedByUserId, campaignId);
            if (!canManage)
            {
                return (false, "Bạn không có quyền quản lý hạn mức trong đợt này.");
            }
        }

        var assignmentCount=await _context.StudentAssignments.CountAsync(a=>a.CampaignId==campaignId&&a.LecturerProfileId==lecturerProfileId);
        if(maxStudents<assignmentCount)return (false,"Hạn mức mới thấp hơn số sinh viên đã phân công.");
        var quota = await _context.LecturerCampaignQuotas
            .FirstOrDefaultAsync(q => q.LecturerProfileId == lecturerProfileId && q.CampaignId == campaignId);

        if (quota == null)
        {
            quota = new LecturerCampaignQuota
            {
                LecturerProfileId = lecturerProfileId,
                CampaignId = campaignId,
                MaxStudents = maxStudents,
                AssignedByUserId = assignedByUserId,
                UpdatedAt = DateTime.UtcNow
            };
            _context.LecturerCampaignQuotas.Add(quota);
        }
        else
        {
            quota.MaxStudents = maxStudents;
            quota.AssignedByUserId = assignedByUserId;
            quota.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return (true, null);
    }
}
