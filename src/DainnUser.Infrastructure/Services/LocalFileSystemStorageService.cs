using DainnUser.Core.Interfaces.Services;
using DainnUser.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DainnUser.Infrastructure.Services;

/// <summary>
/// Local filesystem storage implementation.
/// </summary>
public class LocalFileSystemStorageService : IStorageService
{
    private readonly StorageOptions _options;
    private readonly ILogger<LocalFileSystemStorageService> _logger;

    public LocalFileSystemStorageService(
        IOptions<StorageOptions> options,
        ILogger<LocalFileSystemStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var fullPath = Path.GetFullPath(_options.LocalBasePath);
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            _logger.LogInformation("Created storage directory: {Path}", fullPath);
        }
    }

    public async Task<string> UploadAsync(
        string fileName,
        string contentType,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        var sanitizedFileName = SanitizeFileName(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}_{sanitizedFileName}";
        var fullPath = Path.Combine(Path.GetFullPath(_options.LocalBasePath), uniqueFileName);

        try
        {
            using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fileStream, cancellationToken);
            _logger.LogInformation("Uploaded file to local storage: {Path}", fullPath);

            return GetPublicUrl(uniqueFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to local storage: {FileName}", fileName);
            throw;
        }
    }

    public Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var fileName = Path.GetFileName(fileUrl);
            var fullPath = Path.Combine(Path.GetFullPath(_options.LocalBasePath), fileName);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted file from local storage: {Path}", fullPath);
            }
            else
            {
                _logger.LogWarning("File not found for deletion: {Path}", fullPath);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from local storage: {FileUrl}", fileUrl);
            throw;
        }
    }

    public string GetPublicUrl(string fileName)
    {
        return $"{_options.LocalBaseUrl.TrimEnd('/')}/{fileName}";
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }
}
