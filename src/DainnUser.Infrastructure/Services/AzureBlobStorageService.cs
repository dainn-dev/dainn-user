using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DainnUser.Infrastructure.Services;

/// <summary>
/// Azure Blob Storage implementation.
/// </summary>
public class AzureBlobStorageService : IStorageService
{
    private readonly StorageOptions _options;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly BlobContainerClient _containerClient;

    public AzureBlobStorageService(
        IOptions<StorageOptions> options,
        ILogger<AzureBlobStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var blobServiceClient = new BlobServiceClient(_options.AzureConnectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(_options.AzureContainerName);
        _containerClient.CreateIfNotExists(PublicAccessType.Blob);
    }

    public async Task<string> UploadAsync(
        string fileName,
        string contentType,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var blobClient = _containerClient.GetBlobClient(uniqueFileName);

        try
        {
            var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };
            await blobClient.UploadAsync(
                stream,
                new BlobUploadOptions { HttpHeaders = blobHttpHeaders },
                cancellationToken);

            _logger.LogInformation("Uploaded file to Azure Blob Storage: {FileName}", uniqueFileName);
            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to Azure Blob Storage: {FileName}", fileName);
            throw;
        }
    }

    public async Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = new Uri(fileUrl);
            var fileName = Path.GetFileName(uri.LocalPath);
            var blobClient = _containerClient.GetBlobClient(fileName);

            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            _logger.LogInformation("Deleted file from Azure Blob Storage: {FileName}", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from Azure Blob Storage: {FileUrl}", fileUrl);
            throw;
        }
    }

    public string GetPublicUrl(string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        return blobClient.Uri.ToString();
    }
}
