using InternshipManagement.Web.Models;
using InternshipManagement.Web.ViewModels;

namespace InternshipManagement.Web.Services;

public interface ILecturerProfileService
{
    Task<LecturerProfile?> GetProfileByUserIdAsync(string userId);
    Task<LecturerProfile?> GetProfileByIdAsync(ulong profileId);
    Task<LecturerProfileViewModel?> GetProfileViewModelByUserIdAsync(string userId);
    Task<(bool Success, string? ErrorMessage)> UpdateProfileAsync(string userId, LecturerProfileEditViewModel model);
    Task<LecturerQuotaListViewModel> GetQuotaListViewModelAsync(ulong campaignId, string? managerUserId = null);
    Task<(bool Success, string? ErrorMessage)> SetQuotaAsync(ulong lecturerProfileId, ulong campaignId, uint maxStudents, string assignedByUserId);
}
