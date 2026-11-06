namespace InternshipManagement.Web.ViewModels.Majors;

public class MajorItemViewModel
{
    public ulong Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class MajorListViewModel
{
    public List<MajorItemViewModel> Majors { get; set; } = new();

    public string? Search { get; set; }
    public string? StatusFilter { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }

    public int TotalPages =>
        (int)Math.Ceiling((double)TotalCount / PageSize);
}