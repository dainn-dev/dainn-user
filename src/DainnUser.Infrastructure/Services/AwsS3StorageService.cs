using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DainnUser.Infrastructure.Services;

/// <summary>
/// AWS S3 storage implementation with SigV4 signing.
/// </summary>
public class AwsS3StorageService : IStorageService
{
    private readonly StorageOptions _options;
    private readonly ILogger<AwsS3StorageService> _logger;
    private readonly HttpClient _httpClient;

    public AwsS3StorageService(
        IOptions<StorageOptions> options,
        ILogger<AwsS3StorageService> logger,
        HttpClient httpClient)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<string> UploadAsync(
        string fileName,
        string contentType,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var url = $"https://{_options.AwsBucketName}.s3.{_options.AwsRegion}.amazonaws.com/{uniqueFileName}";

        try
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            var content = memoryStream.ToArray();

            var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = new ByteArrayContent(content)
            };
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

            SignRequest(request, content, uniqueFileName);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Uploaded file to AWS S3: {FileName}", uniqueFileName);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to AWS S3: {FileName}", fileName);
            throw;
        }
    }

    public async Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = new Uri(fileUrl);
            var fileName = Path.GetFileName(uri.LocalPath);

            var request = new HttpRequestMessage(HttpMethod.Delete, fileUrl);
            SignRequest(request, Array.Empty<byte>(), fileName);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Deleted file from AWS S3: {FileName}", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from AWS S3: {FileUrl}", fileUrl);
            throw;
        }
    }

    public string GetPublicUrl(string fileName)
    {
        return $"https://{_options.AwsBucketName}.s3.{_options.AwsRegion}.amazonaws.com/{fileName}";
    }

    private void SignRequest(HttpRequestMessage request, byte[] payload, string objectKey)
    {
        var now = DateTime.UtcNow;
        var dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var amzDate = now.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);

        var payloadHash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();

        request.Headers.Add("x-amz-date", amzDate);
        request.Headers.Add("x-amz-content-sha256", payloadHash);

        var canonicalUri = $"/{objectKey}";
        var canonicalQueryString = "";
        var canonicalHeaders = $"host:{_options.AwsBucketName}.s3.{_options.AwsRegion}.amazonaws.com\n" +
                               $"x-amz-content-sha256:{payloadHash}\n" +
                               $"x-amz-date:{amzDate}\n";
        var signedHeaders = "host;x-amz-content-sha256;x-amz-date";

        var canonicalRequest = $"{request.Method}\n{canonicalUri}\n{canonicalQueryString}\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";
        var canonicalRequestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest))).ToLowerInvariant();

        var credentialScope = $"{dateStamp}/{_options.AwsRegion}/s3/aws4_request";
        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n{canonicalRequestHash}";

        var kDate = HMACSHA256.HashData(Encoding.UTF8.GetBytes($"AWS4{_options.AwsSecretAccessKey}"), Encoding.UTF8.GetBytes(dateStamp));
        var kRegion = HMACSHA256.HashData(kDate, Encoding.UTF8.GetBytes(_options.AwsRegion));
        var kService = HMACSHA256.HashData(kRegion, Encoding.UTF8.GetBytes("s3"));
        var kSigning = HMACSHA256.HashData(kService, Encoding.UTF8.GetBytes("aws4_request"));
        var signature = Convert.ToHexString(HMACSHA256.HashData(kSigning, Encoding.UTF8.GetBytes(stringToSign))).ToLowerInvariant();

        var authorizationHeader = $"AWS4-HMAC-SHA256 Credential={_options.AwsAccessKeyId}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";
        request.Headers.Add("Authorization", authorizationHeader);
    }
}
