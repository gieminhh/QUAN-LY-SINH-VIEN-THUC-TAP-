using InternshipManagement.Web.Models;
namespace InternshipManagement.Web.ViewModels;

public class AssignmentViewModel
{
    public List<Campaign> Campaigns { get; set; } = new();
    public Campaign Campaign { get; set; } = null!;
    public List<StudentProfile> Students { get; set; } = new();
    public List<LecturerProfile> Lecturers { get; set; } = new();
    public List<CompanyMentor> Mentors { get; set; } = new();
    public List<StudentAssignment> Assignments { get; set; } = new();
}
