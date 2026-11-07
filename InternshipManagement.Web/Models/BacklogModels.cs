using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace InternshipManagement.Web.Models;

[Table("campaign_windows")]
public class CampaignWindow
{
    [Key] public ulong Id { get; set; }
    public ulong CampaignId { get; set; }
    [Required, MaxLength(30)] public string Stage { get; set; } = "REGISTRATION";
    public DateTime OpensAt { get; set; }
    public DateTime ClosesAt { get; set; }
}
[Table("campaign_eligibility_policies")]
public class CampaignEligibilityPolicy
{
    [Key] public ulong Id { get; set; }
    public ulong CampaignId { get; set; }
    public decimal? MinimumGpa4 { get; set; }
    public decimal? MinimumGpa10 { get; set; }
    public uint? MinimumCredits { get; set; }
    public string? PrerequisiteNote { get; set; }
}
[Table("campaign_exceptions")]
public class CampaignException
{
    [Key] public ulong Id { get; set; }
    public ulong CampaignId { get; set; }
    [Required, MaxLength(255)] public string UserId { get; set; } = "";
    [Required, MaxLength(30)] public string Stage { get; set; } = "REGISTRATION";
    public DateTime ExpiresAt { get; set; }
    [Required, MaxLength(1000)] public string Reason { get; set; } = "";
    [Required, MaxLength(255)] public string ApprovedBy { get; set; } = "";
}
[Table("student_eligibility_evidence")]
public class StudentEligibilityEvidence
{
    [Key] public ulong Id { get; set; }
    public ulong CampaignId { get; set; }
    public ulong StudentProfileId { get; set; }
    public uint Credits { get; set; }
    public bool PrerequisitesMet { get; set; }
    [Required, MaxLength(1000)] public string SourceNote { get; set; } = "";
    [Required, MaxLength(255)] public string RecordedBy { get; set; } = "";
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
[Table("companies")]
public class CompanyRegistration
{
    [Key] public ulong Id { get; set; }
    [Required, MaxLength(255)] public string OwnerUserId { get; set; } = "";
    public ulong MajorId { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; } = "";
    [Required, MaxLength(60)] public string RegistrationCode { get; set; } = "";
    [Required, EmailAddress, MaxLength(256)] public string Email { get; set; } = "";
    [Required, MaxLength(180)] public string ContactName { get; set; } = "";
    [Required, MaxLength(255)] public string Address { get; set; } = "";
    [Required, MaxLength(20)] public string Status { get; set; } = "PENDING";
    [MaxLength(1000)] public string? ReviewReason { get; set; }
    [MaxLength(255)] public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
[Table("company_mentors")]
public class CompanyMentor
{
    [Key] public ulong Id { get; set; }
    public ulong CompanyId { get; set; }
    [Required, MaxLength(255)] public string UserId { get; set; } = "";
    [Required, MaxLength(180)] public string Name { get; set; } = "";
}
[Table("student_assignments")]
public class StudentAssignment
{
    [Key] public ulong Id { get; set; }
    public ulong CampaignId { get; set; }
    public ulong StudentProfileId { get; set; }
    public ulong LecturerProfileId { get; set; }
    public ulong? CompanyMentorId { get; set; }
    [MaxLength(1000)] public string? QuotaOverrideReason { get; set; }
    [Required, MaxLength(255)] public string AssignedBy { get; set; } = "";
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
