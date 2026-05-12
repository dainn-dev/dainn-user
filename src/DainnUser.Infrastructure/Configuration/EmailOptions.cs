namespace DainnUser.Infrastructure.Configuration;

/// <summary>
/// Configuration options for email service.
/// </summary>
public class EmailOptions
{
    /// <summary>
    /// Gets or sets the email provider to use. Supported values: "Smtp", "SendGrid", "AwsSes".
    /// </summary>
    public string Provider { get; set; } = "Smtp";

    /// <summary>
    /// Gets or sets the sender email address.
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender display name.
    /// </summary>
    public string FromName { get; set; } = string.Empty;

    // SMTP settings

    /// <summary>
    /// Gets or sets the SMTP host address.
    /// </summary>
    public string SmtpHost { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SMTP port.
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// Gets or sets the SMTP username.
    /// </summary>
    public string SmtpUsername { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SMTP password.
    /// </summary>
    public string SmtpPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to enable SSL/TLS.
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    // SendGrid settings

    /// <summary>
    /// Gets or sets the SendGrid API key.
    /// </summary>
    public string SendGridApiKey { get; set; } = string.Empty;

    // AWS SES settings

    /// <summary>
    /// Gets or sets the AWS region for SES.
    /// </summary>
    public string AwsRegion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS access key ID.
    /// </summary>
    public string AwsAccessKeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS secret access key.
    /// </summary>
    public string AwsSecretAccessKey { get; set; } = string.Empty;
}
