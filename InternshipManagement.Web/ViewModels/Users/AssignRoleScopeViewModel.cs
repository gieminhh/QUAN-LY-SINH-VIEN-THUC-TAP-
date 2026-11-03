using Microsoft.AspNetCore.Mvc.Rendering;
using InternshipManagement.Web.Models;

namespace InternshipManagement.Web.ViewModels.Users;

public class ScopeItemDto
{
    public ulong Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string ScopeType { get; set; } = string.Empty;
    public string TargetDescription { get; set; } = string.Empty;
    public DateTime GrantedAt { get; set; }
    public string? GrantedByName { get; set; }
}

public class AssignRoleScopeViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public List<string> UserRoles { get; set; } = new();

    // Existing scopes
    public List<ScopeItemDto> ExistingScopes { get; set; } = new();

    // New scope form
    public string SelectedRoleId { get; set; } = string.Empty;
    public string SelectedScopeType { get; set; } = ScopeTypes.Global;
    public ulong? SelectedFacultyId { get; set; }
    public ulong? SelectedMajorId { get; set; }
    public ulong? SelectedCampaignId { get; set; }

    // Dropdown lists
    public List<SelectListItem> RoleOptions { get; set; } = new();
    public List<SelectListItem> FacultyOptions { get; set; } = new();
    public List<SelectListItem> MajorOptions { get; set; } = new();
    public List<SelectListItem> CampaignOptions { get; set; } = new();
}
