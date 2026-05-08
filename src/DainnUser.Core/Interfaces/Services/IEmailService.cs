namespace DainnUser.Core.Interfaces.Services;

/// <summary>
/// Service for sending emails.
/// </summary>
public interface IEmailService
{
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
    /// Sends a two-factor authentication code to the specified email address.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="username">The username of the user.</param>
    /// <param name="code">The two-factor authentication code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendTwoFactorCodeAsync(string email, string username, string code, CancellationToken cancellationToken = default);
}
