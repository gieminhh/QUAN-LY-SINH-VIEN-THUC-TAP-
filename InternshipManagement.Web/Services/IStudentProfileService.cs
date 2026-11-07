using InternshipManagement.Web.Models;
using InternshipManagement.Web.ViewModels;

namespace InternshipManagement.Web.Services;

public interface IStudentProfileService
{
    Task<StudentProfile?> GetProfileByUserIdAsync(string userId);
    Task<StudentProfile?> GetProfileByIdAsync(ulong profileId);
    Task<StudentProfileViewModel?> GetProfileViewModelByUserIdAsync(string userId);
    Task<(bool Success, string? ErrorMessage)> UpdateProfileAsync(string userId, string? skills, string? interests, IFormFile? cvFile);
    Task<FileAttachment?> GetCvAttachmentAsync(ulong attachmentId);
}
