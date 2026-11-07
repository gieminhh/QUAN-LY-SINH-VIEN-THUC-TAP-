using System.ComponentModel.DataAnnotations;
using InternshipManagement.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InternshipManagement.Web.ViewModels;

public class TopicListViewModel
{
    public List<Topic> Topics { get; set; } = new();

    public ulong? CampaignId { get; set; }
    public string? Status { get; set; }
    public string? SearchQuery { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

    public SelectList? CampaignOptions { get; set; }
    public SelectList? StatusOptions { get; set; }
}

public class TopicCreateEditViewModel : IValidatableObject
{
    public ulong Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn đợt thực tập / đồ án")]
    [Display(Name = "Đợt thực tập / đồ án")]
    public ulong CampaignId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã đề tài")]
    [MaxLength(60, ErrorMessage = "Mã đề tài không quá 60 ký tự")]
    [Display(Name = "Mã đề tài")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên đề tài")]
    [MaxLength(300, ErrorMessage = "Tên đề tài không quá 300 ký tự")]
    [Display(Name = "Tên đề tài")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Mô tả chi tiết")]
    public string? Description { get; set; }

    [Display(Name = "Mục tiêu")]
    public string? Objectives { get; set; }

    [Display(Name = "Yêu cầu kiến thức")]
    public string? KnowledgeRequirements { get; set; }

    [Display(Name = "Công nghệ sử dụng")]
    public string? Technologies { get; set; }

    [Display(Name = "Lĩnh vực / Hướng nghiên cứu")]
    [MaxLength(180)]
    public string? ResearchField { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số sinh viên tối đa")]
    [Range(1, 10, ErrorMessage = "Số sinh viên tối đa từ 1 đến 10")]
    [Display(Name = "Số sinh viên tối đa")]
    public uint MaxStudents { get; set; } = 1;

    [Display(Name = "Tài liệu đính kèm (PDF, DOC, DOCX, ZIP, RAR)")]
    public IFormFile? DocumentFile { get; set; }

    public FileAttachment? ExistingAttachment { get; set; }

    // Option to submit immediately for approval vs save draft
    public bool SubmitImmediately { get; set; } = false;

    public string CurrentStatus { get; set; } = TopicStatuses.Draft;

    public SelectList? CampaignOptions { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CampaignId == 0)
            yield return new ValidationResult("Vui lòng chọn đợt thực tập / đồ án.", new[] { nameof(CampaignId) });
        if (DocumentFile != null)
        {
            var allowed = new[] { ".pdf", ".doc", ".docx", ".zip", ".rar" };
            if (DocumentFile.Length == 0 || DocumentFile.Length > 10 * 1024 * 1024)
                yield return new ValidationResult("Tệp phải có dữ liệu và không vượt quá 10MB.", new[] { nameof(DocumentFile) });
            if (!allowed.Contains(Path.GetExtension(DocumentFile.FileName).ToLowerInvariant()))
                yield return new ValidationResult("Chỉ chấp nhận PDF, DOC, DOCX, ZIP hoặc RAR.", new[] { nameof(DocumentFile) });
        }
    }
}

public class TopicDetailsViewModel
{
    public Topic Topic { get; set; } = null!;
    public List<TopicApprovalHistory> Histories { get; set; } = new();
    public FileAttachment? DocumentAttachment { get; set; }

    public bool CanEdit { get; set; }
    public bool CanSubmit { get; set; }
    public bool CanReview { get; set; }
}
