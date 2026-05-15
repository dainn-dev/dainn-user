using DainnUser.Core.Configuration;
using DainnUser.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DainnUser.Infrastructure.Services;

/// <summary>
/// Service implementation for sending emails using configured provider.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IEmailProvider _provider;
    private readonly ILogger<EmailService> _logger;
    private readonly EmailSubjectsOptions _subjects;
    private readonly DainnUserOptions _userOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailService"/> class.
    /// </summary>
    /// <param name="provider">The email provider.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="subjects">Tenant-customizable email subjects.</param>
    /// <param name="userOptions">DainnUser options (token expirations).</param>
    public EmailService(
        IEmailProvider provider,
        ILogger<EmailService> logger,
        IOptions<EmailSubjectsOptions> subjects,
        DainnUserOptions userOptions)
    {
        _provider = provider;
        _logger = logger;
        _subjects = subjects.Value;
        _userOptions = userOptions;
    }

    /// <inheritdoc/>
    public async Task SendEmailAsync(
        string toEmail,
        string? toName,
        string subject,
        string htmlBody,
        IEnumerable<EmailAttachment>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _provider.SendEmailAsync(toEmail, toName, subject, htmlBody, attachments, cancellationToken);
            _logger.LogInformation("Email sent successfully to {Email} with subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} with subject: {Subject}", toEmail, subject);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SendEmailVerificationAsync(string email, string username, string verificationToken, CancellationToken cancellationToken = default)
    {
        var body = BuildEmailVerificationTemplate(username, verificationToken, _userOptions.EmailVerificationTokenExpirationHours);

        await SendEmailAsync(email, _subjects.EmailVerification, body, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SendPasswordResetAsync(string email, string username, string resetToken, CancellationToken cancellationToken = default)
    {
        var body = BuildPasswordResetTemplate(username, resetToken);

        await SendEmailAsync(email, _subjects.PasswordReset, body, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SendTwoFactorCodeAsync(string email, string username, string code, CancellationToken cancellationToken = default)
    {
        var body = BuildTwoFactorCodeTemplate(username, code);

        await SendEmailAsync(email, _subjects.TwoFactorCode, body, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SendPasswordChangedNotificationAsync(string email, string username, CancellationToken cancellationToken = default)
    {
        var body = BuildPasswordChangedTemplate(username);

        await SendEmailAsync(email, _subjects.PasswordChanged, body, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SendAccountLockoutNotificationAsync(string email, string username, DateTime lockoutEnd, CancellationToken cancellationToken = default)
    {
        var body = BuildAccountLockoutTemplate(username, lockoutEnd);

        await SendEmailAsync(email, _subjects.AccountLockout, body, cancellationToken);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        try
        {
            await _provider.SendEmailAsync(toEmail, null, subject, htmlBody, null, cancellationToken);
            _logger.LogInformation("Email sent successfully to {Email} with subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} with subject: {Subject}", toEmail, subject);
            throw;
        }
    }

    private static string BuildEmailVerificationTemplate(string username, string verificationToken, int expirationHours)
    {
        var expiresPhrase = expirationHours == 1 ? "1 hour" : $"{expirationHours} hours";
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
                            <p>This token will expire in {expiresPhrase}.</p>
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

    private static string BuildPasswordChangedTemplate(string username)
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
                        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
                        .warning {{ color: #d32f2f; font-weight: bold; }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h1>Password Changed</h1>
                        </div>
                        <div class=""content"">
                            <p>Hello {username},</p>
                            <p>Your account password was successfully changed.</p>
                            <p class=""warning"">If you did not make this change, your account may be compromised. Please contact support immediately or reset your password again.</p>
                        </div>
                        <div class=""footer"">
                            <p>This is an automated security notification, please do not reply.</p>
                        </div>
                    </div>
                </body>
                </html>";
    }

    private static string BuildAccountLockoutTemplate(string username, DateTime lockoutEnd)
    {
        var lockoutEndDisplay = lockoutEnd.ToString("u");
        return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=""utf-8"">
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #d32f2f; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f9f9f9; }}
                        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
                        .warning {{ color: #d32f2f; font-weight: bold; }}
                        .when {{ background-color: #f0f0f0; padding: 10px; border-radius: 4px; font-family: monospace; }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h1>Account Locked</h1>
                        </div>
                        <div class=""content"">
                            <p>Hello {username},</p>
                            <p>Your account has been temporarily locked because of multiple failed sign-in attempts.</p>
                            <p>The lock will be released automatically at:</p>
                            <div class=""when"">{lockoutEndDisplay}</div>
                            <p class=""warning"">If you didn't try to sign in, someone else may know your password — please change it as soon as the lock is released, or contact support to unlock your account immediately.</p>
                        </div>
                        <div class=""footer"">
                            <p>This is an automated security notification, please do not reply.</p>
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
