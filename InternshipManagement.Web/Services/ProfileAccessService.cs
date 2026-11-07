using Microsoft.EntityFrameworkCore;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using System.Security.Claims;
namespace InternshipManagement.Web.Services;

public class ProfileAccessService(ApplicationDbContext db, IUserScopeService scopes)
{
    public async Task<bool> StudentAsync(ClaimsPrincipal actor, StudentProfile profile)
    {
        var id = actor.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (profile.UserId == id || actor.IsInRole(AppRoles.Admin)) return true;
        if (actor.IsInRole(AppRoles.TrainingManager) || actor.IsInRole(AppRoles.FacultyManager))
        {
            if (await scopes.CanManageMajorAsync(id, profile.MajorId)) return true;
            var campaigns = await scopes.FilterAccessibleCampaignsAsync(id, db.Campaigns);
            return await db.StudentAssignments.AnyAsync(a => a.StudentProfileId == profile.Id && campaigns.Any(c => c.Id == a.CampaignId));
        }
        if (actor.IsInRole(AppRoles.Lecturer)) return await db.StudentAssignments.AnyAsync(a => a.StudentProfileId == profile.Id && db.LecturerProfiles.Any(l => l.Id == a.LecturerProfileId && l.UserId == id));
        if (actor.IsInRole(AppRoles.Company)) return await db.StudentAssignments.AnyAsync(a => a.StudentProfileId == profile.Id && db.CompanyMentors.Any(m => m.Id == a.CompanyMentorId && m.UserId == id && db.Companies.Any(c => c.Id == m.CompanyId && c.Status == "APPROVED")));
        return false;
    }
    public async Task<bool> LecturerAsync(ClaimsPrincipal actor, LecturerProfile profile)
    {
        var id = actor.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (profile.UserId == id || actor.IsInRole(AppRoles.Admin)) return true;
        if (!actor.IsInRole(AppRoles.TrainingManager) && !actor.IsInRole(AppRoles.FacultyManager)) return false;
        var majors = await scopes.FilterManageableMajorsAsync(id, db.Majors);
        return await majors.AnyAsync(m => m.FacultyId == profile.FacultyId);
    }
}
