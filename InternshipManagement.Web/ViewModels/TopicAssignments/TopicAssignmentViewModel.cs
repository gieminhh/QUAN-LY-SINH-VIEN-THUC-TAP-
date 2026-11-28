
using Microsoft.AspNetCore.Mvc.Rendering;
using InternshipManagement.Web.Models;

namespace InternshipManagement.Web.ViewModels.TopicAssignments;

public class TopicAssignmentViewModel
{
    public ulong? SelectedTopicId { get; set; }

    public string? SelectedStudentUserId { get; set; }

    public ulong? SelectedGroupId { get; set; }

    public string? AdjustmentReason { get; set; }

    public List<SelectListItem> Topics { get; set; } = new();

    public List<SelectListItem> Students { get; set; } = new();

    public List<SelectListItem> Groups { get; set; } = new();
    public List<TopicStudentAssignment> Assignments { get; set; } = new();
}
