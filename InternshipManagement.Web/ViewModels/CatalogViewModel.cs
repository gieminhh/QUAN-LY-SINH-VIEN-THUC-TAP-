using System.ComponentModel.DataAnnotations;
namespace InternshipManagement.Web.ViewModels;

public class CatalogViewModel
{
    public string Kind { get; set; } = "faculty";
    public ulong Id { get; set; }
    [Required, MaxLength(30)] public string Code { get; set; } = "";
    [MaxLength(200)] public string? Name { get; set; }
    public ulong ParentId { get; set; }
    public byte TermNo { get; set; } = 1;
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(6);
    public bool IsActive { get; set; } = true;
    public List<CatalogRow> Rows { get; set; } = new();
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Parents { get; set; } = new();
}
public record CatalogRow(ulong Id, string Code, string Name);
