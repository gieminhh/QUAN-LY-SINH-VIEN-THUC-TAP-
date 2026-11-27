using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels.Assignments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminAssignmentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ITopicAssignmentService _assignmentService;

    public AdminAssignmentsController(
        ApplicationDbContext context,
        ITopicAssignmentService assignmentService)
    {
        _context = context;
        _assignmentService = assignmentService;
    }

    [HttpGet]
    public async Task<IActionResult> Unmatched()
    {
        var model = new UnmatchedAssignmentsViewModel
        {
            Assignments = await _context.TopicStudentAssignments
                .Include(assignment => assignment.StudentUser)
                .Include(assignment => assignment.Campaign)
                .Where(assignment =>
                    assignment.Status == MatchStatus.PendingManual ||
                    assignment.Status == MatchStatus.Unmatched)
                .OrderBy(assignment => assignment.CampaignId)
                .ThenBy(assignment => assignment.StudentUser!.FullName)
                .ToListAsync(),
            Topics = await _context.Topics
                .Where(topic => topic.ApprovalStatus == TopicApprovalStatuses.Approved)
                .OrderBy(topic => topic.Title)
                .ToListAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManualAssign(string studentId, ulong topicId)
    {
        var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Id == topicId);
        if (topic is null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đề tài.";
            return RedirectToAction(nameof(Unmatched));
        }

        var pending = await _context.TopicStudentAssignments.AnyAsync(assignment =>
            assignment.StudentUserId == studentId &&
            assignment.CampaignId == topic.CampaignId &&
            (assignment.Status == MatchStatus.PendingManual || assignment.Status == MatchStatus.Unmatched));
        if (!pending)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sinh viên chưa ghép trong đợt của đề tài.";
            return RedirectToAction(nameof(Unmatched));
        }

        var result = await _assignmentService.AssignAsync(studentId, topicId);
        if (!result.ok)
        {
            TempData["ErrorMessage"] = result.error ?? "Không thể ghép sinh viên vào đề tài.";
            return RedirectToAction(nameof(Unmatched));
        }

        TempData["SuccessMessage"] = "Đã ghép sinh viên vào đề tài.";
        return RedirectToAction(nameof(Unmatched));
    }
}
