namespace DainnUser.Core.Interfaces.Services;

/// <summary>
/// Interface for avatar management operations.
/// </summary>
public interface IAvatarService
{
    /// <summary>
    /// Uploads and processes an avatar image.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="fileName">Original file name (for extension).</param>
    /// <param name="contentType">The content type of the file.</param>
    /// <param name="stream">The file stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The URL of the uploaded avatar.</returns>
    Task<string> UploadAvatarAsync(
        Guid userId,
        string fileName,
        string contentType,
        Stream stream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an avatar from storage.
    /// </summary>
    /// <param name="avatarUrl">The avatar URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAvatarAsync(string avatarUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the public URL for an avatar.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <returns>The public URL.</returns>
    string GetAvatarUrl(string fileName);
}
