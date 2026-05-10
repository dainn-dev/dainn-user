using DainnUser.Core.Models.Authentication;

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
    /// Authenticates a user with email and password, issuing JWT access and refresh tokens.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password.</param>
    /// <param name="ipAddress">The client IP address (for login history and session metadata). Optional.</param>
    /// <param name="userAgent">The client user agent (for login history and session metadata). Optional.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="LoginResult"/> containing tokens and user info.</returns>
    /// <exception cref="DainnUser.Core.Exceptions.InvalidCredentialsException">
    /// Thrown when the email/password combination is invalid.
    /// </exception>
    /// <exception cref="DainnUser.Core.Exceptions.EmailNotVerifiedException">
    /// Thrown when email verification is required but the user's email is not verified.
    /// </exception>
    /// <exception cref="DainnUser.Core.Exceptions.AccountLockedException">
    /// Thrown when the account is currently locked out.
    /// </exception>
    /// <exception cref="DainnUser.Core.Exceptions.AccountInactiveException">
    /// Thrown when the account status prevents authentication (suspended, deactivated, etc.).
    /// </exception>
    Task<LoginResult> LoginAsync(
        string email,
        string password,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a fresh access + refresh token pair given a still-valid refresh token, rotating
    /// (one-time-use) the presented token. Reuse of an already-consumed token is treated as
    /// a security incident and revokes all of the user's active refresh tokens and sessions.
    /// </summary>
    /// <param name="refreshToken">The plain refresh token previously issued to the client.</param>
    /// <param name="ipAddress">Client IP address (recorded on the rotated session). Optional.</param>
    /// <param name="userAgent">Client user agent (recorded on the rotated session). Optional.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="LoginResult"/> with the new access and refresh tokens.</returns>
    /// <exception cref="DainnUser.Core.Exceptions.InvalidRefreshTokenException">
    /// Thrown when the refresh token is unknown, revoked, expired, or has already been used (reuse).
    /// </exception>
    /// <exception cref="DainnUser.Core.Exceptions.AccountInactiveException">
    /// Thrown when the user's account is suspended, deactivated, or locked.
    /// </exception>
    /// <exception cref="DainnUser.Core.Exceptions.AccountLockedException">
    /// Thrown when the account is currently locked out due to repeated login failures.
    /// </exception>
    Task<LoginResult> RefreshTokenAsync(
        string refreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs the user out of a single session: deactivates the session row and revokes the
    /// associated refresh token so it cannot be exchanged again. Idempotent — calling with
    /// an unknown or already-inactive session is a no-op.
    /// </summary>
    /// <param name="sessionId">The session identifier (typically the JWT <c>sid</c> claim).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogoutAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually clears a user's account lockout state (admin operation). Resets
    /// <see cref="DainnUser.Core.Entities.User.FailedLoginAttempts"/> to 0, clears
    /// <see cref="DainnUser.Core.Entities.User.LockoutEnd"/>, and if the user's status was
    /// <see cref="DainnUser.Core.Enums.UserStatus.Locked"/> restores it to <c>Active</c>.
    /// Idempotent — calling on an unlocked user is a no-op.
    /// </summary>
    /// <param name="userId">The user to unlock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when a user was found and processed; false when the user does not exist.</returns>
    Task<bool> UnlockAccountAsync(Guid userId, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Initiates a password reset flow. Sends a password reset token to the given email address if an
    /// active account exists. Always completes successfully regardless of whether the email is found,
    /// to prevent user enumeration.
    /// </summary>
    /// <param name="email">The email address of the account to reset.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a password reset using the token previously sent via email. Validates the token,
    /// updates the password, invalidates all active refresh tokens and sessions, and sends a
    /// confirmation notification to the account owner.
    /// </summary>
    /// <param name="token">The plain-text password reset token from the email.</param>
    /// <param name="newPassword">The new password to set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when the reset succeeded; false when the token is invalid or expired.</returns>
    /// <exception cref="DainnUser.Core.Exceptions.InvalidPasswordResetTokenException">
    /// Thrown when the token is invalid, expired, already used, or revoked.
    /// </exception>
    Task ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default);
}
