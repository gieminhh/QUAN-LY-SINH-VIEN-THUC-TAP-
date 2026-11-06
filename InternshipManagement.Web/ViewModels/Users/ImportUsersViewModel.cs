using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Web.ViewModels.Users;

public class ImportUsersViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn file Excel.")]
    public IFormFile? ExcelFile { get; set; }

    public List<ImportUserRowViewModel> Rows { get; set; } = new();
}

public class ImportUserRowViewModel
{
    public int RowNumber { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string Role { get; set; } = string.Empty;

    public bool IsValid { get; set; }

    public string? ErrorMessage { get; set; }

    public string? TemporaryPassword { get; set; }
}
