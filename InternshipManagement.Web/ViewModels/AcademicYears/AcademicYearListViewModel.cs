
namespace InternshipManagement.Web.ViewModels.AcademicYears;

public class AcademicYearItemViewModel
{
    public ulong Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class AcademicYearListViewModel
{
    public List<AcademicYearItemViewModel> AcademicYears { get; set; } = new();

    public string? Search { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }

    public int TotalPages =>
        (int)Math.Ceiling((double)TotalCount / PageSize);
}
