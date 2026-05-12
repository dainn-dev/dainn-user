namespace DainnUser.Core.Interfaces.Services;

/// <summary>
/// Interface for file storage providers.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Uploads a file to storage.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="contentType">The content type.</param>
    /// <param name="stream">The file stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The URL or path to the uploaded file.</returns>
    Task<string> UploadAsync(string fileName, string contentType, Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from storage.
    /// </summary>
    /// <param name="fileUrl">The file URL or path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the public URL for a file.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <returns>The public URL.</returns>
    string GetPublicUrl(string fileName);
}
