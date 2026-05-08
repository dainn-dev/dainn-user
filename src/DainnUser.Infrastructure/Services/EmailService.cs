using DainnUser.Core.Interfaces.Services;
using DainnUser.Infrastructure.Configuration;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DainnUser.Infrastructure.Services;

/// <summary>
/// Service implementation for sending emails.
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<EmailService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailService"/> class.
    /// </summary>
    /// <param name="emailOptions">The email configuration options.</param>
    /// <param name="logger">The logger.</param>
    public EmailService(IOptions<EmailOptions> emailOptions, ILogger<EmailService> logger)
    {
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task SendEmailVerificationAsync(string email, string username, string verificationToken, CancellationToken cancellationToken = default)
    {
        var subject = "Verify Your Email Address";
        var body = BuildEmailVerificationTemplate(username, verificationToken);

        await SendEmailAsync(email, subject, body, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SendPasswordResetAsync(string email, string username, string resetToken, CancellationToken cancellationToken = default)
    {
        var subject = "Reset Your Password";
        var body = BuildPasswordResetTemplate(username, resetToken);

        await SendEmailAsync(email, subject, body, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SendTwoFactorCodeAsync(string email, string username, string code, CancellationToken cancellationToken = default)
    {
        var subject = "Your Two-Factor Authentication Code";
        var body = BuildTwoFactorCodeTemplate(username, code);

        await SendEmailAsync(email, subject, body, cancellationToken);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailOptions.FromName, _emailOptions.FromEmail));
            message.To.Add(new MailboxAddress(string.Empty, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_emailOptions.SmtpHost, _emailOptions.SmtpPort, _emailOptions.EnableSsl, cancellationToken);

            if (!string.IsNullOrEmpty(_emailOptions.SmtpUsername) && !string.IsNullOrEmpty(_emailOptions.SmtpPassword))
            {
                await client.AuthenticateAsync(_emailOptions.SmtpUsername, _emailOptions.SmtpPassword, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Email} with subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} with subject: {Subject}", toEmail, subject);
            throw;
        }
    }

    private static string BuildEmailVerificationTemplate(string username, string verificationToken)
    {
        return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=""utf-8"">
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f9f9f9; }}
                        .button {{ display: inline-block; padding: 12px 24px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
                        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
                        .token {{ background-color: #f0f0f0; padding: 10px; border-radius: 4px; font-family: monospace; word-break: break-all; }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h1>Email Verification</h1>
                        </div>
                        <div class=""content"">
                            <p>Hello {username},</p>
                            <p>Thank you for registering! Please verify your email address to activate your account.</p>
                            <p>Your verification token is:</p>
                            <div class=""token"">{verificationToken}</div>
                            <p>This token will expire in 24 hours.</p>
                            <p>If you didn't create an account, please ignore this email.</p>
                        </div>
                        <div class=""footer"">
                            <p>This is an automated message, please do not reply.</p>
                        </div>
                    </div>
                </body>
                </html>";
    }

    private static string BuildPasswordResetTemplate(string username, string resetToken)
    {
        return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=""utf-8"">
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #FF9800; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f9f9f9; }}
                        .button {{ display: inline-block; padding: 12px 24px; background-color: #FF9800; color: white; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
                        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
                        .token {{ background-color: #f0f0f0; padding: 10px; border-radius: 4px; font-family: monospace; word-break: break-all; }}
                        .warning {{ color: #d32f2f; font-weight: bold; }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h1>Password Reset</h1>
                        </div>
                        <div class=""content"">
                            <p>Hello {username},</p>
                            <p>We received a request to reset your password. Use the token below to reset your password:</p>
                            <div class=""token"">{resetToken}</div>
                            <p>This token will expire in 1 hour.</p>
                            <p class=""warning"">If you didn't request a password reset, please ignore this email and ensure your account is secure.</p>
                        </div>
                        <div class=""footer"">
                            <p>This is an automated message, please do not reply.</p>
                        </div>
                    </div>
                </body>
                </html>";
    }

    private static string BuildTwoFactorCodeTemplate(string username, string code)
    {
        return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=""utf-8"">
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #2196F3; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f9f9f9; }}
                        .code {{ font-size: 32px; font-weight: bold; text-align: center; padding: 20px; background-color: #f0f0f0; border-radius: 4px; letter-spacing: 8px; }}
                        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h1>Two-Factor Authentication</h1>
                        </div>
                        <div class=""content"">
                            <p>Hello {username},</p>
                            <p>Your two-factor authentication code is:</p>
                            <div class=""code"">{code}</div>
                            <p>This code will expire in 5 minutes.</p>
                            <p>If you didn't request this code, please secure your account immediately.</p>
                        </div>
                        <div class=""footer"">
                            <p>This is an automated message, please do not reply.</p>
                        </div>
                    </div>
                </body>
                </html>";
    }
}
