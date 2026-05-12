namespace DainnUser.Core.Interfaces.Services;

/// <summary>
/// Service for sending emails.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a generic email with optional attachments.
    /// </summary>
    Task SendEmailAsync(
        string toEmail,
        string? toName,
        string subject,
        string htmlBody,
        IEnumerable<EmailAttachment>? attachments = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email verification message to the specified email address.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="username">The username of the user.</param>
    /// <param name="verificationToken">The verification token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendEmailVerificationAsync(string email, string username, string verificationToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password reset email to the specified email address.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="username">The username of the user.</param>
    /// <param name="resetToken">The password reset token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendPasswordResetAsync(string email, string username, string resetToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a confirmation that the user's password has been changed (typically via the reset
    /// flow). The email contains no token — only an alert so the legitimate owner can react if
    /// the change was unexpected.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="username">The username of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPasswordChangedNotificationAsync(string email, string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to the user that their account has been locked due to repeated failed
    /// login attempts. The user does NOT receive credentials or a token — only a heads-up so they
    /// can take action (change password, etc.) if the lockout was unexpected.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="username">The username of the user.</param>
    /// <param name="lockoutEnd">When the lockout will expire (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAccountLockoutNotificationAsync(string email, string username, DateTime lockoutEnd, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a two-factor authentication code to the specified email address.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="username">The username of the user.</param>
    /// <param name="code">The two-factor authentication code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendTwoFactorCodeAsync(string email, string username, string code, CancellationToken cancellationToken = default);
}
