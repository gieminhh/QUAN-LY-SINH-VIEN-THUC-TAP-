using Microsoft.EntityFrameworkCore;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.ViewModels;

namespace InternshipManagement.Web.Services;

public class StudentProfileService : IStudentProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;

    public StudentProfileService(ApplicationDbContext context, IFileStorageService fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }

    public async Task<StudentProfile?> GetProfileByUserIdAsync(string userId)
    {
        return await _context.StudentProfiles
            .Include(p => p.User)
            .Include(p => p.Major)
                .ThenInclude(m => m!.Faculty)
            .Include(p => p.Class)
            .Include(p => p.GpaRecords)
                .ThenInclude(g => g.Term)
                    .ThenInclude(t => t!.AcademicYear)
            .Include(p => p.Attachments)
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<StudentProfile?> GetProfileByIdAsync(ulong profileId)
    {
        return await _context.StudentProfiles
            .Include(p => p.User)
            .Include(p => p.Major)
                .ThenInclude(m => m!.Faculty)
            .Include(p => p.Class)
            .Include(p => p.GpaRecords)
                .ThenInclude(g => g.Term)
                    .ThenInclude(t => t!.AcademicYear)
            .Include(p => p.Attachments)
            .FirstOrDefaultAsync(p => p.Id == profileId);
    }

    public async Task<StudentProfileViewModel?> GetProfileViewModelByUserIdAsync(string userId)
    {
        var profile = await GetProfileByUserIdAsync(userId);
        if (profile == null) return null;

        var latestGpa = profile.GpaRecords
            .OrderByDescending(g => g.Term?.StartDate ?? DateTime.MinValue)
            .FirstOrDefault();

        var cvAttachment = profile.Attachments
            .Where(a => a.FileCategory == FileCategories.StudentCv)
            .OrderByDescending(a => a.UploadedAt)
            .FirstOrDefault();

        return new StudentProfileViewModel
        {
            Id = profile.Id,
            UserId = profile.UserId,
            FullName = profile.User?.FullName ?? string.Empty,
            StudentCode = profile.StudentCode,
            Email = profile.User?.Email ?? string.Empty,
            PhoneNumber = profile.User?.PhoneNumber,
            ClassName = profile.Class != null ? $"{profile.Class.Code} - {profile.Class.Name}" : "Chưa phân lớp",
            MajorName = profile.Major?.Name ?? "Chưa phân ngành",
            FacultyName = profile.Major?.Faculty?.Name ?? "Chưa phân khoa",
            Cohort = profile.Class?.Cohort,
            LatestGpa4 = latestGpa?.Gpa4,
            LatestGpa10 = latestGpa?.Gpa10,
            LatestTermName = latestGpa?.Term != null ? $"HK{latestGpa.Term.TermNo} ({latestGpa.Term.AcademicYear?.Code})" : null,
            GpaHistory = profile.GpaRecords.OrderByDescending(g => g.Term?.StartDate).ToList(),
            Skills = profile.Skills,
            Interests = profile.Interests,
            CvAttachment = cvAttachment
        };
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateProfileAsync(string userId, string? skills, string? interests, IFormFile? cvFile)
    {
        var profile = await _context.StudentProfiles
            .Include(p => p.Attachments)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            return (false, "Không tìm thấy hồ sơ sinh viên tương ứng.");
        }

        if (cvFile != null)
        {
            var extension = Path.GetExtension(cvFile.FileName).ToLowerInvariant();
            if (cvFile.Length == 0 || cvFile.Length > 10 * 1024 * 1024 || !new[] { ".pdf", ".doc", ".docx" }.Contains(extension))
                return (false, "CV phải là PDF, DOC hoặc DOCX, có dữ liệu và không vượt quá 10MB.");
        }

        // Only update allowed fields (Skills, Interests)
        profile.Skills = skills;
        profile.Interests = interests;
        profile.UpdatedAt = DateTime.UtcNow;

        // Process CV file upload if provided
        if (cvFile != null && cvFile.Length > 0)
        {
            var saveResult = await _fileStorage.SaveFileAsync(cvFile, "student_cvs");
            if (!saveResult.Success)
            {
                return (false, saveResult.ErrorMessage);
            }

            // Remove or deactivate previous CV attachment if exists
            var previousCv = profile.Attachments.FirstOrDefault(a => a.FileCategory == FileCategories.StudentCv);
            if (previousCv != null)
            {
                _fileStorage.DeleteFile(previousCv.StorageKey);
                _context.FileAttachments.Remove(previousCv);
            }

            var newAttachment = new FileAttachment
            {
                StudentProfileId = profile.Id,
                FileCategory = FileCategories.StudentCv,
                OriginalFileName = cvFile.FileName,
                StorageKey = saveResult.StorageKey!,
                ContentType = cvFile.ContentType ?? "application/octet-stream",
                SizeBytes = (ulong)cvFile.Length,
                UploadedByUserId = userId,
                UploadedAt = DateTime.UtcNow
            };

            _context.FileAttachments.Add(newAttachment);
        }

        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<FileAttachment?> GetCvAttachmentAsync(ulong attachmentId)
    {
        return await _context.FileAttachments
            .Include(a => a.StudentProfile)
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.FileCategory == FileCategories.StudentCv);
    }
}
