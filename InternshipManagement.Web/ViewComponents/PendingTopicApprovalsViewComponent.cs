using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Web.ViewComponents;

public class PendingTopicApprovalsViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserScopeService _scopeService;

    public PendingTopicApprovalsViewComponent(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IUserScopeService scopeService)
    {
        _context = context;
        _userManager = userManager;
        _scopeService = scopeService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var user = await _userManager.GetUserAsync(
            HttpContext.User);

        if (user == null || !user.IsActive)
            return View(0);

        var roles = await _userManager.GetRolesAsync(user);

        var canReview = roles.Any(role =>
            role == AppRoles.Admin ||
            role == AppRoles.TrainingManager ||
            role == AppRoles.FacultyManager);

        if (!canReview)
            return View(0);

        var reviewableCampaigns =
            await _scopeService.FilterReviewableCampaignsAsync(
                user.Id,
                _context.Campaigns.AsNoTracking());

        var campaignIds = await reviewableCampaigns
            .Select(c => c.Id)
            .ToListAsync();

        if (campaignIds.Count == 0)
            return View(0);

        var pendingCount = await _context.Topics
            .AsNoTracking()
            .CountAsync(t =>
                campaignIds.Contains(t.CampaignId) &&
                t.ApprovalStatus == TopicApprovalStatuses.Pending);

        return View(pendingCount);
    }
}
