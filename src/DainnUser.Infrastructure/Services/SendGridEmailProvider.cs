using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace DainnUser.Infrastructure.Services;

/// <summary>
/// SendGrid email provider implementation via SendGrid Mail Send API v3.
/// </summary>
public class SendGridEmailProvider : IEmailProvider
{
    private readonly EmailOptions _options;
    private readonly HttpClient _httpClient;

    public SendGridEmailProvider(IOptions<EmailOptions> options, HttpClient httpClient)
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
        var payload = new SendGridMessage
        {
            From = new SendGridContact { Email = _options.FromEmail, Name = _options.FromName },
            Subject = subject,
            Content = new List<SendGridContent>
            {
                new() { Type = "text/html", Value = htmlBody }
            },
            Personalizations = new List<SendGridPersonalization>
            {
                new()
                {
                    To = new List<SendGridContact>
                    {
                        new() { Email = toEmail, Name = toName }
                    }
                }
            }
        };

        if (attachments is not null)
        {
            payload.Attachments = attachments.Select(a => new SendGridAttachment
            {
                FileName = a.FileName,
                Type = a.ContentType,
                Content = Convert.ToBase64String(a.Content),
                Disposition = "attachment"
            }).ToList();
        }

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("Authorization", $"Bearer {_options.SendGridApiKey}");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private class SendGridMessage
    {
        [JsonPropertyName("from")]
        public SendGridContact From { get; set; } = null!;

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public List<SendGridContent> Content { get; set; } = new();

        [JsonPropertyName("personalizations")]
        public List<SendGridPersonalization> Personalizations { get; set; } = new();

        [JsonPropertyName("attachments")]
        public List<SendGridAttachment>? Attachments { get; set; }
    }

    private class SendGridContact
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private class SendGridContent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "text/html";

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }

    private class SendGridPersonalization
    {
        [JsonPropertyName("to")]
        public List<SendGridContact> To { get; set; } = new();
    }

    private class SendGridAttachment
    {
        [JsonPropertyName("filename")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "application/octet-stream";

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("disposition")]
        public string Disposition { get; set; } = "attachment";
    }
}
