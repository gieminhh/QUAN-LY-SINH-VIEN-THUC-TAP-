using InternshipManagement.Web.Models;

namespace InternshipManagement.Web.ViewModels.Assignments;

public class UnmatchedAssignmentsViewModel
{
    public List<TopicStudentAssignment> Assignments { get; set; } = new();
    public List<Topic> Topics { get; set; } = new();
}
