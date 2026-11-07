using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;

namespace InternshipManagement.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserScopeService _scopeService;

    public HomeController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IUserScopeService scopeService)
    {
        _context = context;
        _userManager = userManager;
        _scopeService = scopeService;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var roles = await _userManager.GetRolesAsync(user);
        ViewBag.UserRoles = roles;
        ViewBag.FullName = user.FullName;

        var isAdmin = roles.Contains(AppRoles.Admin);
        var isManager = isAdmin || roles.Contains(AppRoles.TrainingManager) || roles.Contains(AppRoles.FacultyManager);
        var isLecturer = roles.Contains(AppRoles.Lecturer);
        var isStudent = roles.Contains(AppRoles.Student);

        ViewBag.ApprovedTopicsCount = await _context.Topics.CountAsync(t => t.Status == TopicStatuses.Approved);

        if (isAdmin)
        {
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.PendingTopicsCount = await _context.Topics.CountAsync(t => t.Status == TopicStatuses.Pending);
        }

        if (isManager)
        {
            var accessibleQuery = await _scopeService.FilterAccessibleCampaignsAsync(user.Id, _context.Campaigns);
            var accessibleCampaignIds = await accessibleQuery.Select(c => c.Id).ToListAsync();

            ViewBag.TotalCampaigns = await accessibleQuery.CountAsync();
            ViewBag.OpenCampaigns = await accessibleQuery.CountAsync(c => c.Status == CampaignStatuses.Open);
            ViewBag.AccessibleCampaignsCount = ViewBag.TotalCampaigns;
            ViewBag.UserScopes = await _scopeService.GetUserScopesAsync(user.Id);
            ViewBag.PendingTopicsCount = await _context.Topics.CountAsync(t => accessibleCampaignIds.Contains(t.CampaignId) && t.Status == TopicStatuses.Pending);
        }

        if (isLecturer)
        {
            var lecturerProfile = await _context.LecturerProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (lecturerProfile != null)
            {
                ViewBag.MyTopicsCount = await _context.Topics.CountAsync(t => t.LecturerProfileId == lecturerProfile.Id);
                ViewBag.MyApprovedTopicsCount = await _context.Topics.CountAsync(t => t.LecturerProfileId == lecturerProfile.Id && t.Status == TopicStatuses.Approved);
            }
        }

        if (isStudent)
        {
            ViewBag.HasStudentProfile = await _context.StudentProfiles.AnyAsync(p => p.UserId == user.Id);
        }

        return View();
    }
}
