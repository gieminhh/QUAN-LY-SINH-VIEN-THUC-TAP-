namespace InternshipManagement.Web.ViewModels.Faculties;

public class FacultyItemViewModel
{
    public ulong Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int MajorCount { get; set; }
}

public class FacultyListViewModel
{
    public List<FacultyItemViewModel> Faculties { get; set; } = new();

    public string? Search { get; set; }
    public string? StatusFilter { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }

    public int TotalPages =>
    PageSize > 0
        ? (int)Math.Ceiling((double)TotalCount / PageSize)
        : 0;
}