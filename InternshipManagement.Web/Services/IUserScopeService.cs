using InternshipManagement.Web.Models;

namespace InternshipManagement.Web.Services;

public interface IUserScopeService
{
    Task<List<UserRoleScope>> GetUserScopesAsync(string userId);
    Task<bool> HasGlobalScopeAsync(string userId, string? roleName = null);
    Task<bool> CanManageCampaignAsync(string userId, ulong campaignId);
    Task<bool> CanManageMajorAsync(string userId, ulong majorId);
    Task<IQueryable<Major>> FilterManageableMajorsAsync(string userId, IQueryable<Major> query);
    Task<IQueryable<Campaign>> FilterAccessibleCampaignsAsync(string userId, IQueryable<Campaign> query);
    Task<bool> AssignScopeAsync(string userId, string roleId, string scopeType, ulong? facultyId, ulong? majorId, ulong? campaignId, string grantedByUserId);
    Task<bool> RemoveScopeAsync(ulong scopeId);
}
