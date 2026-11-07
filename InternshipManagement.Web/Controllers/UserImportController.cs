using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels;
namespace InternshipManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class UserImportController(UserImportWorkflow workflow) : Controller
{
    string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpGet] public IActionResult Index() => View(new UserImportViewModel());
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(3_000_000)]
    public async Task<IActionResult> Preview(IFormFile? file) => View("Index", (await workflow.Preview(Actor, file)).Model);
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(string ticket)
    {
        var result = await workflow.Confirm(Actor, ticket);
        return result == null ? BadRequest("Phiếu nhập không hợp lệ hoặc đã hết hạn.") : View("Index", result.Model);
    }
}
