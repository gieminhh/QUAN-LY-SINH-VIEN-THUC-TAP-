using Microsoft.AspNetCore.Mvc.Rendering;

namespace InternshipManagement.Web.ViewModels.Topics;

public class TopicListViewModel
{
    public string? Search { get; set; }

    public ulong? CampaignFilter { get; set; }
    
    public ulong? AcademicYearFilter { get; set; }

    public ulong? FacultyFilter { get; set; }

    public string? LecturerFilter { get; set; }

    public List<SelectListItem> AcademicYearOptions { get; set; } = new();

    public List<SelectListItem> FacultyOptions { get; set; } = new();

    public List<SelectListItem> LecturerOptions { get; set; } = new();

    public string TypeFilter { get; set; } = "ALL";

    public string StatusFilter { get; set; } = "ALL";

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalCount { get; set; }

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public List<SelectListItem> CampaignOptions { get; set; } = new();

    public List<TopicListItemViewModel> Topics { get; set; } = new();
}

public class TopicListItemViewModel
{
    public ulong Id { get; set; }

    public ulong CampaignId { get; set; }

    public string CampaignName { get; set; } = string.Empty;

    public string CampaignKind { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string TopicType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int? MaxStudents { get; set; }

    public int? MaxGroups { get; set; }

    public string CreatedByName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool CanEdit { get; set; }
}
