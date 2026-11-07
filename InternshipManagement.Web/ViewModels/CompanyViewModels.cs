using InternshipManagement.Web.Models;
namespace InternshipManagement.Web.ViewModels;

public class CompaniesViewModel
{
    public List<CompanyRegistration> Companies { get; set; } = new();
    public List<Major> Majors { get; set; } = new();
    public List<CompanyMentor> Mentors { get; set; } = new();
}
public record AssignedStudentRow(ulong Id, string Code, string Name, string Campaign);
