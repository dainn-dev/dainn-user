using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon;
using Amazon.Runtime;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace DainnUser.Infrastructure.Services;

/// <summary>
/// AWS SES email provider implementation.
/// </summary>
public class AwsSesEmailProvider : IEmailProvider
{
    private readonly EmailOptions _options;
    private readonly HttpClient _httpClient;

    public AwsSesEmailProvider(IOptions<EmailOptions> options, HttpClient httpClient)
    {
        _options = options.Value;
        _httpClient = httpClient;
    }

    public async Task SendEmailAsync(
        string toEmail,
        string? toName,
        string subject,
        string htmlBody,
        IEnumerable<EmailAttachment>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        var region = RegionEndpoint.GetBySystemName(_options.AwsRegion);
        var endpoint = $"https://email.{region.SystemName}.amazonaws.com";

        var toAddresses = string.IsNullOrEmpty(toName)
            ? toEmail
            : $"{toName} <{toEmail}>";

        var rawMessage = BuildRawEmailMessage(toEmail, toAddresses, subject, htmlBody, attachments);

        var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/v3/email/outbound-emails")
        {
            Content = new StringContent(rawMessage, Encoding.UTF8, "application/json")
        };

        SignRequest(request, endpoint, rawMessage);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private string BuildRawEmailMessage(
        string toEmail,
        string toAddresses,
        string subject,
        string htmlBody,
        IEnumerable<EmailAttachment>? attachments)
    {
        var msg = $@"From: {toAddresses}
To: {toAddresses}
Subject: =?UTF-8?B?{Convert.ToBase64String(Encoding.UTF8.GetBytes(subject))}?=
MIME-Version: 1.0
Content-Type: text/html; charset=UTF-8
";

        if (attachments is null)
        {
            msg += $"\r\n{htmlBody}";
        }
        else
        {
            var boundary = "----=_Part_" + Guid.NewGuid().ToString("N");
            msg = $@"From: {toAddresses}
To: {toAddresses}
Subject: =?UTF-8?B?{Convert.ToBase64String(Encoding.UTF8.GetBytes(subject))}?=
MIME-Version: 1.0
Content-Type: multipart/mixed; boundary=""{boundary}""

--{boundary}
Content-Type: text/html; charset=UTF-8

{htmlBody}
";
            foreach (var att in attachments)
            {
                var b64 = Convert.ToBase64String(att.Content);
                msg += $@"--{boundary}
Content-Type: {att.ContentType}; name=""{att.FileName}""
Content-Disposition: attachment; filename=""{att.FileName}""
Content-Transfer-Encoding: base64

{b64}
";
            }

            msg += $"--{boundary}--\r\n";
        }

        return JsonSerializer.Serialize(new { Raw = new { Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(msg)) } });
    }

    private void SignRequest(HttpRequestMessage request, string endpoint, string payload)
    {
        var now = DateTime.UtcNow;
        var amzDate = now.ToString("yyyyMMddTHHmmssZ");
        var dateStamp = now.ToString("yyyyMMdd");

        request.Headers.Add("X-Amz-Date", amzDate);
        request.Headers.Add("X-Amz-Target", "AWSSESv2.SendEmail");
        request.Headers.Add("Content-Type", "application/x-amz-json-1.1");

        var service = "ses";
        var method = "POST";
        var canonicalUri = "/v3/email/outbound-emails";
        var canonicalQuerystring = "";
        var payloadHash = ComputeSha256Hash(payload);

        var canonicalHeaders =
            $"content-type:application/x-amz-json-1.1\n" +
            $"host:email.{_options.AwsRegion}.amazonaws.com\n" +
            $"x-amz-date:{amzDate}\n" +
            $"x-amz-target:AWSSESv2.SendEmail\n";

        var signedHeaders = "content-type;host;x-amz-date;x-amz-target";
        var canonicalRequest = $"{method}\n{canonicalUri}\n{canonicalQuerystring}\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";

        var algorithm = "AWS4-HMAC-SHA256";
        var credentialScope = $"{dateStamp}/{_options.AwsRegion}/{service}/aws4_request";
        var stringToSign = $"{algorithm}\n{amzDate}\n{credentialScope}\n{ComputeSha256Hash(canonicalRequest)}";

        var signingKey = GetSignatureKey(
            _options.AwsSecretAccessKey, dateStamp, _options.AwsRegion, service);
        var signature = ToHexString(HmacSha256(signingKey, stringToSign));

        var authHeader =
            $"{algorithm} Credential={_options.AwsAccessKeyId}/{credentialScope}, " +
            $"SignedHeaders={signedHeaders}, Signature={signature}";

        request.Headers.Add("Authorization", authHeader);
    }

    private static string ComputeSha256Hash(string raw)
    {
        var bytes = Encoding.UTF8.GetBytes(raw);
        var hash = SHA256.HashData(bytes);
        return ToHexString(hash);
    }

    private static string ToHexString(byte[] data)
    {
        return Convert.ToHexString(data).ToLowerInvariant();
    }

    private static byte[] HmacSha256(byte[] key, string data)
    {
        return System.Security.Cryptography.HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(data));
    }

    private static byte[] GetSignatureKey(string key, string dateStamp, string region, string service)
    {
        var kDate = HmacSha256(Encoding.UTF8.GetBytes("AWS4" + key), dateStamp);
        var kRegion = HmacSha256(kDate, region);
        var kService = HmacSha256(kRegion, service);
        var kSigning = HmacSha256(kService, "aws4_request");
        return kSigning;
    }
}
