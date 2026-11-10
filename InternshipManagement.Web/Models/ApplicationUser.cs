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

    [MaxLength(100)]
    public string? ClassName { get; set; }

    public ulong? MajorId { get; set; }

    [MaxLength(2000)]
    public string? Skills { get; set; }

    [MaxLength(2000)]
    public string? Interests { get; set; }

    [MaxLength(255)]
    public string? CvStoredFileName { get; set; }

    [MaxLength(1000)]
    public string? LecturerSpecialty { get; set; }

    // Navigations
    public virtual ICollection<UserRoleScope> RoleScopes { get; set; } = new List<UserRoleScope>();
    public virtual ICollection<Campaign> CreatedCampaigns { get; set; } = new List<Campaign>();
    public virtual ICollection<UserRoleScope> GrantedRoleScopes { get; set; } = new List<UserRoleScope>();
    public virtual Major? Major { get; set; }
    public virtual ICollection<LecturerCampaignQuota> LecturerCampaignQuotas { get; set; } = new List<LecturerCampaignQuota>();
    public virtual ICollection<Topic> CreatedTopics { get; set; } = new List<Topic>();
    public virtual ICollection<TopicLecturer> TopicLectures { get; set; } = new List<TopicLecturer>();
    public virtual ICollection<TopicAttachment> UploadedTopicAttachments { get; set; } = new List<TopicAttachment>();
}
