using InternshipManagement.Web.Data;
using InternshipManagement.Web.ViewModels.TopicAssignments;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;

namespace InternshipManagement.Web.Controllers;

[Authorize]
public class TopicAssignmentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ITopicAssignmentService _assignmentService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserScopeService _scopeService;

    public TopicAssignmentsController(
    ITopicAssignmentService assignmentService,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext db,
    IUserScopeService scopeService)
    {
        _assignmentService = assignmentService;
        _userManager = userManager;
        _db = db;
        _scopeService = scopeService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var staffUserId = _userManager.GetUserId(User);

        if (string.IsNullOrEmpty(staffUserId))
            return Unauthorized();

        var allTopics = await _db.Topics
            .AsNoTracking()
            .ToListAsync();

        var allowedTopics = new List<Topic>();

        foreach (var topic in allTopics)
        {
            if (await _scopeService.CanManageCampaignAsync(
                staffUserId, topic.CampaignId))
            {
                allowedTopics.Add(topic);
            }
        }

        var students = await _userManager.GetUsersInRoleAsync(AppRoles.Student);
        var allowedTopicIds = allowedTopics.Select(t => t.Id).ToList();
        var groups = await _db.TopicAssignmentGroups
            .AsNoTracking()
            .Where(g => allowedTopicIds.Contains(g.TopicId))
            .OrderBy(g => g.GroupName)
            .ToListAsync();

        var assignments = await _db.TopicStudentAssignments
            .AsNoTracking()
            .Include(a => a.Topic)
            .Include(a => a.StudentUser)
            .Include(a => a.Group)
            .Where(a => a.TopicId.HasValue &&
                        allowedTopicIds.Contains(a.TopicId.Value))
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync();

        var viewModel = new TopicAssignmentViewModel
        {
            Assignments = assignments,
            Topics = allowedTopics.Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.Title
            }).ToList(),

            Students = students.Select(s => new SelectListItem
            {
                Value = s.Id,
                Text = s.Email ?? s.UserName ?? s.Id
            }).ToList(),

            Groups = groups.Select(g => new SelectListItem
            {
                Value = g.Id.ToString(),
                Text = $"{g.GroupName} ({allowedTopics.FirstOrDefault(t => t.Id == g.TopicId)?.Title ?? $"Đề tài #{g.TopicId}"})"
            }).ToList()
            
        };
            ViewBag.GroupsJson = JsonSerializer.Serialize(
                            groups.Select(g => new
                            {
                                id = g.Id.ToString(),
                                text = g.GroupName + " (" +
                                    (allowedTopics.FirstOrDefault(t => t.Id == g.TopicId)?.Title
                                        ?? ("Đề tài #" + g.TopicId)) + ")",
                                topicId = g.TopicId.ToString()
                            })
                        );
        return View(viewModel);
    }

    // Ghép sinh viên vào đề tài
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(
        ulong topicId,
        string studentUserId,
        string? adjustmentReason,
        ulong? groupId,
        string? groupName)
    {
        var staffUserId = _userManager.GetUserId(User);

        if (string.IsNullOrEmpty(staffUserId))
            return Unauthorized();

        var result = await _assignmentService.AssignAsync(
            topicId,
            studentUserId,
            staffUserId,
            adjustmentReason,
            groupId,
            groupName);

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Ghép sinh viên thành công.";
        return RedirectToAction(nameof(Index));
    }

    // Gỡ cặp ghép
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(
        ulong assignmentId,
        string adjustmentReason)
    {
        var staffUserId = _userManager.GetUserId(User);

        if (string.IsNullOrEmpty(staffUserId))
            return Unauthorized();

        var result = await _assignmentService.RemoveAsync(
            assignmentId,
            staffUserId,
            adjustmentReason);

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Gỡ cặp ghép thành công.";
        return RedirectToAction(nameof(Index));
    }

    
    // Khóa cặp ghép
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(
        ulong assignmentId,
        string? adjustmentReason)
    {
        var staffUserId = _userManager.GetUserId(User);

        if (string.IsNullOrEmpty(staffUserId))
            return Unauthorized();

        var result = await _assignmentService.LockAsync(
            assignmentId, staffUserId, adjustmentReason);

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Khóa cặp ghép thành công.";
        return RedirectToAction(nameof(Index));
    }

    // Mở khóa cặp ghép
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(
        ulong assignmentId,
        string? adjustmentReason)
    {
        var staffUserId = _userManager.GetUserId(User);

        if (string.IsNullOrEmpty(staffUserId))
            return Unauthorized();

        var result = await _assignmentService.UnlockAsync(
            assignmentId, staffUserId, adjustmentReason);

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Mở khóa cặp ghép thành công.";
        return RedirectToAction(nameof(Index));
    }

}
