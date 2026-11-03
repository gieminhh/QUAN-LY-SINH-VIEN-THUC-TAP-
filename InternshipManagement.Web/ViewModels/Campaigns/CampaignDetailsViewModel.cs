using InternshipManagement.Web.Models;

namespace InternshipManagement.Web.ViewModels.Campaigns;

public class CampaignDetailsViewModel
{
    public ulong Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string KindDisplayName => Kind == CampaignKinds.Project ? "Đồ án tốt nghiệp" : "Thực tập doanh nghiệp";
    public string MajorName { get; set; } = string.Empty;
    public string MajorCode { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;
    public string TermName { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public string TrainingSystem { get; set; } = "CHINH_QUY";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = CampaignStatuses.Draft;
    public string CreatedByUserName { get; set; } = string.Empty;
    public string CreatedByFullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public bool CanEdit { get; set; }
    public bool CanChangeStatus { get; set; }
}
