using System.ComponentModel.DataAnnotations;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.ViewModels.TopicProposals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Web.Controllers;

[Authorize(Roles = $"{AppRoles.Student},{AppRoles.Lecturer}")]
public class TopicProposalsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TopicProposalsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !user.IsActive)
            return Forbid();

        var isLecturer = User.IsInRole(AppRoles.Lecturer);
        var proposals = await _context.TopicProposals
            .AsNoTracking()
            .Where(p => isLecturer
                ? p.LecturerUserId == user.Id
                : p.StudentUserId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new TopicProposalListItemViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Objectives = p.Objectives,
                StudentNote = p.StudentNote,
                ReviewFeedback = p.ReviewFeedback,
                Status = p.Status,
                CampaignName = p.Campaign!.Name,
                StudentName = p.Student!.FullName,
                LecturerName = p.Lecturer!.FullName,
                CreatedAt = p.CreatedAt,
                ReviewedAt = p.ReviewedAt
            })
            .ToListAsync();

        return View(new TopicProposalListViewModel
        {
            IsLecturer = isLecturer,
            Proposals = proposals
        });
    }

    [Authorize(Roles = AppRoles.Student)]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var user = await GetActiveStudentAsync();
        if (user == null)
            return Forbid();

        var model = new TopicProposalCreateViewModel();
        await PopulateAssignmentOptionsAsync(model, user.Id);
        return View(model);
    }

    [Authorize(Roles = AppRoles.Student)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TopicProposalCreateViewModel model)
    {
        var user = await GetActiveStudentAsync();
        if (user == null)
            return Forbid();

        var assignment = await GetEligibleAssignmentQuery(user.Id)
            .FirstOrDefaultAsync(a => a.Id == model.AssignmentId);

        if (assignment == null)
        {
            ModelState.AddModelError(nameof(model.AssignmentId),
                "Đợt đào tạo hoặc giảng viên không còn được phân công cho bạn.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateAssignmentOptionsAsync(model, user.Id);
            return View(model);
        }

        var proposal = new TopicProposal
        {
            CampaignId = assignment!.CampaignId,
            StudentUserId = user.Id,
            LecturerUserId = assignment.LecturerUserId,
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            Objectives = NormalizeOptionalText(model.Objectives),
            StudentNote = NormalizeOptionalText(model.StudentNote),
            Status = TopicProposalStatuses.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TopicProposals.Add(proposal);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã gửi đề xuất đề tài cho giảng viên.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Lecturer)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(
        ulong id,
        string decision,
        [StringLength(2000, ErrorMessage = "Phản hồi không được vượt quá 2000 ký tự.")]
        string? reviewFeedback)
    {
        var lecturer = await _userManager.GetUserAsync(User);
        if (lecturer == null || !lecturer.IsActive)
            return Forbid();

        if (decision != TopicProposalStatuses.Approved
            && decision != TopicProposalStatuses.Rejected)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Phản hồi vượt quá giới hạn 2000 ký tự.";
            return RedirectToAction(nameof(Index));
        }

        var proposal = await _context.TopicProposals
            .FirstOrDefaultAsync(p => p.Id == id && p.LecturerUserId == lecturer.Id);

        if (proposal == null)
            return NotFound();

        if (proposal.Status != TopicProposalStatuses.Pending)
        {
            TempData["Error"] = "Đề xuất này đã được xử lý trước đó.";
            return RedirectToAction(nameof(Index));
        }

        var now = DateTime.UtcNow;
        proposal.Status = decision;
        proposal.ReviewFeedback = NormalizeOptionalText(reviewFeedback);
        proposal.ReviewedAt = now;
        proposal.UpdatedAt = now;

        await _context.SaveChangesAsync();

        TempData["Success"] = decision == TopicProposalStatuses.Approved
            ? "Đã chấp nhận đề xuất."
            : "Đã từ chối đề xuất.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ApplicationUser?> GetActiveStudentAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !user.IsActive
            || !await _userManager.IsInRoleAsync(user, AppRoles.Student))
        {
            return null;
        }

        return user;
    }

    private IQueryable<LecturerCampaignAssignment> GetEligibleAssignmentQuery(string studentId)
    {
        var today = DateTime.Today;
        return _context.LecturerCampaignAssignments
            .AsNoTracking()
            .Include(a => a.Campaign)
            .Include(a => a.Lecturer)
            .Where(a => a.StudentUserId == studentId
                && (a.Status == LecturerAssignmentStatuses.Assigned
                    || a.Status == LecturerAssignmentStatuses.ApprovedException)
                && a.Campaign != null
                && a.Campaign.Status == CampaignStatuses.Open
                && a.Campaign.StartDate <= today
                && a.Campaign.EndDate >= today
                && a.Lecturer != null
                && a.Lecturer.IsActive);
    }

    private async Task PopulateAssignmentOptionsAsync(
        TopicProposalCreateViewModel model,
        string studentId)
    {
        model.AssignmentOptions = await GetEligibleAssignmentQuery(studentId)
            .OrderBy(a => a.Campaign!.Name)
            .ThenBy(a => a.Lecturer!.FullName)
            .Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.Campaign!.Name} — {a.Lecturer!.FullName}"
            })
            .ToListAsync();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
