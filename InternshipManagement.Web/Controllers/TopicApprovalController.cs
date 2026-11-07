using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels;

namespace InternshipManagement.Web.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.TrainingManager},{AppRoles.FacultyManager}")]
public class TopicApprovalController : Controller
{
    private readonly ITopicApprovalService _approvalService;
    private readonly UserManager<ApplicationUser> _userManager;

    public TopicApprovalController(
        ITopicApprovalService approvalService,
        UserManager<ApplicationUser> userManager)
    {
        _approvalService = approvalService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(ulong? campaignId, string? status = TopicStatuses.Pending, ulong? majorId = null, string? search = null, int page = 1)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();

        var model = await _approvalService.GetTopicsForApprovalViewModelAsync(currentUserId, campaignId, status, majorId, search, page, 10);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Review(ulong id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();

        var canReview = await _approvalService.CanReviewTopicAsync(currentUserId, id);
        if (!canReview)
        {
            TempData["ErrorMessage"] = "Bạn không có quyền xét duyệt đề tài này (ngoài phạm vi quản lý khoa/ngành/đợt được phân công).";
            return RedirectToAction(nameof(Index));
        }

        var model = await _approvalService.GetTopicReviewViewModelAsync(id, currentUserId);
        if (model == null) return NotFound("Không tìm thấy đề tài.");

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(ulong id, string? comments)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();

        var result = await _approvalService.ApproveTopicAsync(id, currentUserId, comments);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Review), new { id });
        }

        TempData["SuccessMessage"] = "Phê duyệt đề tài thành công! Sinh viên hiện có thể nhìn thấy đề tài này.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestRevision(ulong id, string comments)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();

        if (string.IsNullOrWhiteSpace(comments))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập lý do hoặc nội dung yêu cầu chỉnh sửa đề tài.";
            return RedirectToAction(nameof(Review), new { id });
        }

        var result = await _approvalService.RequestRevisionAsync(id, currentUserId, comments);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Review), new { id });
        }

        TempData["SuccessMessage"] = "Đã gửi yêu cầu chỉnh sửa đến giảng viên hướng dẫn.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(ulong id, string comments)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();

        if (string.IsNullOrWhiteSpace(comments))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối đề tài.";
            return RedirectToAction(nameof(Review), new { id });
        }

        var result = await _approvalService.RejectTopicAsync(id, currentUserId, comments);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Review), new { id });
        }

        TempData["SuccessMessage"] = "Đã từ chối đề tài thành công.";
        return RedirectToAction(nameof(Index));
    }
}
