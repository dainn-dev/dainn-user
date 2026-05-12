using DainnUser.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;

namespace DainnUser.Infrastructure.Services;

/// <summary>
/// Service for avatar upload, validation, and processing.
/// </summary>
public class AvatarService : IAvatarService
{
    private readonly IStorageService _storageService;
    private readonly ILogger<AvatarService> _logger;

    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

    public AvatarService(IStorageService storageService, ILogger<AvatarService> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<string> UploadAvatarAsync(
        Guid userId,
        string fileName,
        string contentType,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ValidateFile(fileName, contentType, stream);

        try
        {
            using var originalStream = stream;
            using var image = await Image.LoadAsync(originalStream, cancellationToken);

            var outFileName = $"avatar_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}.jpg";
            var resizedStream = new MemoryStream();

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(400, 400),
                Mode = ResizeMode.Crop
            }));

            await image.SaveAsJpegAsync(resizedStream, cancellationToken);
            resizedStream.Position = 0;

            var avatarUrl = await _storageService.UploadAsync(
                outFileName,
                "image/jpeg",
                resizedStream,
                cancellationToken);

            _logger.LogInformation("Avatar uploaded for user {UserId}: {AvatarUrl}", userId, avatarUrl);
            return avatarUrl;
        }
        catch (ImageFormatException ex)
        {
            _logger.LogWarning(ex, "Invalid image format uploaded by user {UserId}", userId);
            throw new ArgumentException("The uploaded file is not a valid image.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload avatar for user {UserId}", userId);
            throw;
        }
    }

    public async Task DeleteAvatarAsync(string avatarUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl))
        {
            return;
        }

        try
        {
            await _storageService.DeleteAsync(avatarUrl, cancellationToken);
            _logger.LogInformation("Avatar deleted: {AvatarUrl}", avatarUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete avatar: {AvatarUrl}", avatarUrl);
            throw;
        }
    }

    public string GetAvatarUrl(string fileName)
    {
        return _storageService.GetPublicUrl(fileName);
    }

    private static void ValidateFile(string fileName, string contentType, Stream stream)
    {
        if (stream == null || stream.Length == 0)
        {
            throw new ArgumentException("No file uploaded.");
        }

        if (!AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
        {
            throw new ArgumentException(
                $"Invalid file type. Only {string.Join(", ", AllowedContentTypes)} are allowed.");
        }

        if (stream.Length > MaxFileSizeBytes)
        {
            throw new ArgumentException(
                $"File size exceeds the maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)}MB.");
        }
    }
}
