namespace DainnUser.Core.Interfaces.Services;

/// <summary>
/// Interface for email provider implementations.
/// </summary>
public interface IEmailProvider
{
    /// <summary>
    /// Sends an email message.
    /// </summary>
    /// <param name="toEmail">The recipient email address.</param>
    /// <param name="toName">The recipient name (optional).</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="htmlBody">The HTML body content.</param>
    /// <param name="attachments">Optional list of attachments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendEmailAsync(
        string toEmail,
        string? toName,
        string subject,
        string htmlBody,
        IEnumerable<EmailAttachment>? attachments = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an email attachment.
/// </summary>
public class EmailAttachment
{
    /// <summary>
    /// Gets or sets the filename.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content type (MIME type).
    /// </summary>
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>
    /// Gets or sets the file content as byte array.
    /// </summary>
    public byte[] Content { get; set; } = Array.Empty<byte>();
}
