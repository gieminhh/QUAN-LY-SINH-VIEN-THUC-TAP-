using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;

namespace InternshipManagement.Web.Services;

public class UserScopeService : IUserScopeService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserScopeService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<List<UserRoleScope>> GetUserScopesAsync(string userId)
    {
        return await _context.UserRoleScopes
            .Include(s => s.Role)
            .Include(s => s.Faculty)
            .Include(s => s.Major)
            .Include(s => s.Campaign)
            .Include(s => s.GrantedByUser)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.GrantedAt)
            .ToListAsync();
    }

    public async Task<bool> HasGlobalScopeAsync(string userId, string? roleName = null)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        if (await _userManager.IsInRoleAsync(user, AppRoles.Admin))
            return true;

        var query = _context.UserRoleScopes
            .Where(s => s.UserId == userId && s.ScopeType == ScopeTypes.Global);

        if (!string.IsNullOrEmpty(roleName))
        {
            query = query.Where(s => s.Role != null && s.Role.Name == roleName);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> CanManageMajorAsync(string userId, ulong majorId)
    {
        var query = await FilterManageableMajorsAsync(userId, _context.Majors);
        return await query.AnyAsync(m => m.Id == majorId);
    }

    public async Task<IQueryable<Major>> FilterManageableMajorsAsync(string userId, IQueryable<Major> query)
    {
        if (await HasGlobalScopeAsync(userId))
            return query;

        var scopes = await _context.UserRoleScopes
            .Where(s => s.UserId == userId)
            .ToListAsync();

        var facultyIds = scopes.Where(s => s.ScopeType == ScopeTypes.Faculty && s.FacultyId.HasValue)
            .Select(s => s.FacultyId!.Value).ToList();
        var majorIds = scopes.Where(s => s.ScopeType == ScopeTypes.Major && s.MajorId.HasValue)
            .Select(s => s.MajorId!.Value).ToList();

        // A scope for one campaign does not grant permission to create other campaigns in its major.
        return query.Where(m => facultyIds.Contains(m.FacultyId) || majorIds.Contains(m.Id));
    }

    public async Task<bool> CanManageCampaignAsync(string userId, ulong campaignId)
    {
        if (await HasGlobalScopeAsync(userId))
            return true;

        var campaign = await _context.Campaigns
            .Include(c => c.Major)
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        if (campaign == null) return false;

        var scopes = await _context.UserRoleScopes
            .Where(s => s.UserId == userId)
            .ToListAsync();

        return scopes.Any(s =>
            s.ScopeType == ScopeTypes.Global ||
            (s.ScopeType == ScopeTypes.Campaign && s.CampaignId == campaignId) ||
            (s.ScopeType == ScopeTypes.Major && s.MajorId == campaign.MajorId) ||
            (s.ScopeType == ScopeTypes.Faculty && campaign.Major != null && s.FacultyId == campaign.Major.FacultyId)
        );
    }

    public async Task<IQueryable<Campaign>> FilterAccessibleCampaignsAsync(string userId, IQueryable<Campaign> query)
    {
        if (await HasGlobalScopeAsync(userId))
            return query;

        var scopes = await _context.UserRoleScopes
            .Where(s => s.UserId == userId)
            .ToListAsync();

        if (!scopes.Any())
        {
            // If user has no assigned scopes, return empty result
            return query.Where(c => false);
        }

        var facultyIds = scopes.Where(s => s.ScopeType == ScopeTypes.Faculty && s.FacultyId.HasValue).Select(s => s.FacultyId!.Value).ToList();
        var majorIds = scopes.Where(s => s.ScopeType == ScopeTypes.Major && s.MajorId.HasValue).Select(s => s.MajorId!.Value).ToList();
        var campaignIds = scopes.Where(s => s.ScopeType == ScopeTypes.Campaign && s.CampaignId.HasValue).Select(s => s.CampaignId!.Value).ToList();

        return query.Where(c =>
            campaignIds.Contains(c.Id) ||
            majorIds.Contains(c.MajorId) ||
            (c.Major != null && facultyIds.Contains(c.Major.FacultyId))
        );
    }

    public async Task<bool> AssignScopeAsync(
        string userId,
        string roleId,
        string scopeType,
        ulong? facultyId,
        ulong? majorId,
        ulong? campaignId,
        string grantedByUserId)
    {
        // 1. Validate that user has this role in AspNetUserRoles
        var hasUserRole = await _context.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
        if (!hasUserRole)
        {
            return false;
        }

        // 2. Clear invalid fields based on scope type
        switch (scopeType)
        {
            case ScopeTypes.Global:
                facultyId = null;
                majorId = null;
                campaignId = null;
                break;
            case ScopeTypes.Faculty:
                if (!facultyId.HasValue) return false;
                majorId = null;
                campaignId = null;
                break;
            case ScopeTypes.Major:
                if (!majorId.HasValue) return false;
                facultyId = null;
                campaignId = null;
                break;
            case ScopeTypes.Campaign:
                if (!campaignId.HasValue) return false;
                facultyId = null;
                majorId = null;
                break;
            default:
                return false;
        }

        // 3. Check for duplicates
        var duplicate = await _context.UserRoleScopes.AnyAsync(s =>
            s.UserId == userId &&
            s.RoleId == roleId &&
            s.ScopeType == scopeType &&
            s.FacultyId == facultyId &&
            s.MajorId == majorId &&
            s.CampaignId == campaignId);

        if (duplicate)
        {
            return true; // Already assigned
        }

        var newScope = new UserRoleScope
        {
            UserId = userId,
            RoleId = roleId,
            ScopeType = scopeType,
            FacultyId = facultyId,
            MajorId = majorId,
            CampaignId = campaignId,
            GrantedAt = DateTime.UtcNow,
            GrantedByUserId = grantedByUserId
        };

        _context.UserRoleScopes.Add(newScope);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveScopeAsync(ulong scopeId)
    {
        var scope = await _context.UserRoleScopes.FindAsync(scopeId);
        if (scope == null) return false;

        _context.UserRoleScopes.Remove(scope);
        await _context.SaveChangesAsync();
        return true;
    }
}
