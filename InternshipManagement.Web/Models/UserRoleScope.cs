using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

public static class ScopeTypes
{
    public const string Global = "GLOBAL";
    public const string Faculty = "FACULTY";
    public const string Major = "MAJOR";
    public const string Campaign = "CAMPAIGN";

    public static string GetDisplayName(string value) => value switch
    {
        Global => "Toàn trường",
        Faculty => "Theo khoa",
        Major => "Theo ngành",
        Campaign => "Theo đợt",
        _ => value
    };

}

[Table("user_role_scopes")]
public class UserRoleScope
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("UserId")]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("RoleId")]
    public string RoleId { get; set; } = string.Empty;

    [Required]
    [Column("scope_type")]
    public string ScopeType { get; set; } = ScopeTypes.Global;

    [Column("faculty_id")]
    public ulong? FacultyId { get; set; }

    [Column("major_id")]
    public ulong? MajorId { get; set; }

    [Column("campaign_id")]
    public ulong? CampaignId { get; set; }

    /// <summary>
    /// Computed / Generated column in MySQL:
    /// CASE scope_type WHEN 'GLOBAL' THEN 0 WHEN 'FACULTY' THEN faculty_id ...
    /// </summary>
    [Column("ScopeTargetId")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public ulong? ScopeTargetId { get; set; }

    [Column("granted_at")]
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    [Column("granted_by_user_id")]
    public string? GrantedByUserId { get; set; }

    // Navigation
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }

    [ForeignKey(nameof(RoleId))]
    public virtual IdentityRole? Role { get; set; }

    [ForeignKey(nameof(FacultyId))]
    public virtual Faculty? Faculty { get; set; }

    [ForeignKey(nameof(MajorId))]
    public virtual Major? Major { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public virtual Campaign? Campaign { get; set; }

    [ForeignKey(nameof(GrantedByUserId))]
    public virtual ApplicationUser? GrantedByUser { get; set; }
}
