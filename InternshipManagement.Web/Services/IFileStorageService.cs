namespace InternshipManagement.Web.Services;

public interface IFileStorageService
{
    Task<(bool Success, string? StorageKey, string? ErrorMessage)> SaveFileAsync(IFormFile file, string subFolder);
    string GetFullPath(string storageKey);
    bool DeleteFile(string storageKey);
    bool FileExists(string storageKey);
}
