using DainnUser.Core.Interfaces.Services;
using DainnUser.Infrastructure.Configuration;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DainnUser.Infrastructure.Services;

/// <summary>
/// SMTP email provider implementation using MailKit.
/// </summary>
public class SmtpEmailProvider : IEmailProvider
{
    private readonly EmailOptions _options;

    public SmtpEmailProvider(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendEmailAsync(
        string toEmail,
        string? toName,
        string subject,
        string htmlBody,
        IEnumerable<EmailAttachment>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        message.To.Add(new MailboxAddress(toName ?? string.Empty, toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };

        if (attachments is not null)
        {
            foreach (var att in attachments)
            {
                bodyBuilder.Attachments.Add(
                    att.FileName,
                    att.Content,
                    ContentType.Parse(att.ContentType));
            }
        }

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, _options.EnableSsl, cancellationToken);

        if (!string.IsNullOrEmpty(_options.SmtpUsername) && !string.IsNullOrEmpty(_options.SmtpPassword))
        {
            await client.AuthenticateAsync(_options.SmtpUsername, _options.SmtpPassword, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
