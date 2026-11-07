using InternshipManagement.Web.Models;
namespace InternshipManagement.Web.ViewModels;

public class CampaignRulesViewModel
{
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> ExceptionUsers { get; set; } = new();
    public Campaign Campaign { get; set; } = null!;
    public List<Campaign> Campaigns { get; set; } = new();
    public List<CampaignWindow> Windows { get; set; } = new();
    public CampaignEligibilityPolicy Policy { get; set; } = new();
    public List<EligibilityRow> Students { get; set; } = new();
    public List<CampaignException> Exceptions { get; set; } = new();
}
public record EligibilityRow(ulong Id, string Code, string Name, string? Error);
