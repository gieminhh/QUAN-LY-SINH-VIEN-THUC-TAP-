using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Web.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(180)]
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigations
    public virtual ICollection<UserRoleScope> RoleScopes { get; set; } = new List<UserRoleScope>();
    public virtual ICollection<Campaign> CreatedCampaigns { get; set; } = new List<Campaign>();
    public virtual ICollection<UserRoleScope> GrantedRoleScopes { get; set; } = new List<UserRoleScope>();
}
