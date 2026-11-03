using Microsoft.AspNetCore.Mvc.Rendering;
using InternshipManagement.Web.Models;

namespace InternshipManagement.Web.ViewModels.Campaigns;

public class CampaignItemViewModel
{
    public ulong Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string KindDisplayName => Kind == CampaignKinds.Project ? "Đồ án tốt nghiệp" : "Thực tập doanh nghiệp";
    public string MajorName { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;
    public string TermDisplayName { get; set; } = string.Empty;
    public string TrainingSystem { get; set; } = "CHINH_QUY";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = CampaignStatuses.Draft;
    public string CreatedByUserName { get; set; } = string.Empty;
    public bool CanEdit { get; set; }
    public bool CanChangeStatus { get; set; }
}

public class CampaignListViewModel
{
    public List<CampaignItemViewModel> Campaigns { get; set; } = new();

    // Filters
    public string? Search { get; set; }
    public string? KindFilter { get; set; }
    public string? StatusFilter { get; set; }
    public ulong? TermFilter { get; set; }
    public ulong? MajorFilter { get; set; }

    // Dropdowns for filtering
    public List<SelectListItem> TermOptions { get; set; } = new();
    public List<SelectListItem> MajorOptions { get; set; } = new();

    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
