namespace InternshipManagement.Web.ViewModels;

public record ImportUserRow(int Row, string UserName, string Email, string FullName, string Role, string InitialPassword, string? PhoneNumber = null);
public record ImportRowResult(int Row, string UserName, bool Success, string Message);
public class UserImportViewModel
{
    public string? Ticket { get; set; }
    public List<ImportRowResult> Results { get; set; } = new();
    public string? Error { get; set; }
    public bool Finished { get; set; }
}
