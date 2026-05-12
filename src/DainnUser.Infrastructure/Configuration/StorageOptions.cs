namespace DainnUser.Infrastructure.Configuration;

/// <summary>
/// Configuration options for file storage.
/// </summary>
public class StorageOptions
{
    /// <summary>
    /// Storage provider: "Local", "Azure", "AwsS3".
    /// </summary>
    public string Provider { get; set; } = "Local";

    /// <summary>
    /// Local filesystem base path (for Local provider).
    /// </summary>
    public string LocalBasePath { get; set; } = "wwwroot/uploads";

    /// <summary>
    /// Base URL for public access (for Local provider).
    /// </summary>
    public string LocalBaseUrl { get; set; } = "/uploads";

    /// <summary>
    /// Azure Blob Storage connection string.
    /// </summary>
    public string AzureConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Azure Blob Storage container name.
    /// </summary>
    public string AzureContainerName { get; set; } = "avatars";

    /// <summary>
    /// AWS S3 region.
    /// </summary>
    public string AwsRegion { get; set; } = string.Empty;

    /// <summary>
    /// AWS access key ID.
    /// </summary>
    public string AwsAccessKeyId { get; set; } = string.Empty;

    /// <summary>
    /// AWS secret access key.
    /// </summary>
    public string AwsSecretAccessKey { get; set; } = string.Empty;

    /// <summary>
    /// AWS S3 bucket name.
    /// </summary>
    public string AwsBucketName { get; set; } = "avatars";
}
