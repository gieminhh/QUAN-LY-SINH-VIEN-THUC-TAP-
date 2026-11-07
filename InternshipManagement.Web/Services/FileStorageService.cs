namespace InternshipManagement.Web.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly string _uploadBasePath;

    // Allowed extensions
    private readonly string[] _allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".zip", ".rar" };
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public FileStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
        // Secure directory outside wwwroot
        _uploadBasePath = Path.Combine(_environment.ContentRootPath, "App_Data", "Uploads");
        if (!Directory.Exists(_uploadBasePath))
        {
            Directory.CreateDirectory(_uploadBasePath);
        }
    }

    public async Task<(bool Success, string? StorageKey, string? ErrorMessage)> SaveFileAsync(IFormFile file, string subFolder)
    {
        if (file == null || file.Length == 0)
        {
            return (false, null, "File tải lên không hợp lệ hoặc rỗng.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return (false, null, "Dung lượng file vượt quá giới hạn tối đa cho phép (10MB).");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(ext))
        {
            return (false, null, $"Định dạng file không được hỗ trợ. Chỉ chấp nhận các định dạng: {string.Join(", ", _allowedExtensions)}.");
        }

        var targetFolder = Path.Combine(_uploadBasePath, subFolder);
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        // Generate safe unique filename
        var safeFileName = Path.GetFileNameWithoutExtension(file.FileName);
        // Replace dangerous characters
        safeFileName = string.Concat(safeFileName.Split(Path.GetInvalidFileNameChars()));
        if (safeFileName.Length > 50) safeFileName = safeFileName[..50];

        var storageKey = $"{subFolder}/{Guid.NewGuid():N}_{safeFileName}{ext}";
        var fullPath = Path.Combine(_uploadBasePath, storageKey);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return (true, storageKey, null);
    }

    public string GetFullPath(string storageKey)
    {
        // Prevent path traversal
        var normalizedKey = storageKey.Replace('\\', '/').TrimStart('/');
        // Read former profile uploads through the same permission-checked download routes.
        if (normalizedKey.StartsWith("legacy_cvs/", StringComparison.Ordinal))
        {
            var fileName = normalizedKey["legacy_cvs/".Length..];
            if (fileName != Path.GetFileName(fileName) || fileName.Contains(':') || fileName is "" or "." or "..")
                throw new UnauthorizedAccessException("Đường dẫn file không hợp lệ.");
            return Path.Combine(_environment.ContentRootPath, "App_Data", "CVs", fileName);
        }
        var fullPath = Path.GetFullPath(Path.Combine(_uploadBasePath, normalizedKey));
        if (!fullPath.StartsWith(Path.GetFullPath(_uploadBasePath) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Đường dẫn file không hợp lệ.");
        }
        return fullPath;
    }

    public bool DeleteFile(string storageKey)
    {
        try
        {
            var fullPath = GetFullPath(storageKey);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }
        }
        catch
        {
            // Ignore error on deletion
        }
        return false;
    }

    public bool FileExists(string storageKey)
    {
        try
        {
            var fullPath = GetFullPath(storageKey);
            return File.Exists(fullPath);
        }
        catch
        {
            return false;
        }
    }
}
