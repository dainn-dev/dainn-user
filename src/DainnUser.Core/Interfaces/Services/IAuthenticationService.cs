namespace DainnUser.Core.Interfaces.Services;

/// <summary>
/// Service for user authentication operations.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Registers a new user with email verification.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="username">The user's username.</param>
    /// <param name="password">The user's password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created user ID.</returns>
    Task<Guid> RegisterAsync(string email, string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a user's email address using the verification token.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="token">The verification token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if verification was successful, false otherwise.</returns>
    Task<bool> VerifyEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resends the email verification token to a user.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the email was sent successfully, false otherwise.</returns>
    Task<bool> ResendVerificationEmailAsync(string email, CancellationToken cancellationToken = default);
}
