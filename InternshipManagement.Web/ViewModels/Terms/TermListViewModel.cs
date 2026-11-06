
namespace InternshipManagement.Web.ViewModels.Terms;

public class TermItemViewModel
{
    public ulong Id { get; set; }
    public ulong AcademicYearId { get; set; }
    public string AcademicYearCode { get; set; } = string.Empty;
    public byte TermNo { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class TermListViewModel
{
    public List<TermItemViewModel> Terms { get; set; } = new();
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
