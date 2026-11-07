using InternshipManagement.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InternshipManagement.Web.ViewModels;

public class LecturerQuotaListViewModel
{
    public ulong CampaignId { get; set; }
    public Campaign? Campaign { get; set; }
    public SelectList? CampaignOptions { get; set; }

    public List<LecturerQuotaItemViewModel> Lecturers { get; set; } = new();
}

public class LecturerQuotaItemViewModel
{
    public ulong LecturerProfileId { get; set; }
    public string LecturerCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string? AcademicDegree { get; set; }

    public uint MaxStudents { get; set; }
    public int TotalRegisteredTopics { get; set; }
    public int ApprovedTopics { get; set; }
}

public class SetQuotaInputModel
{
    public ulong CampaignId { get; set; }
    public ulong LecturerProfileId { get; set; }
    public uint MaxStudents { get; set; }
}
