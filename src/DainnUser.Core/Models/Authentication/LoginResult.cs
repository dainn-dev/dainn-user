namespace DainnUser.Core.Models.Authentication;

/// <summary>
/// Result returned after a successful authentication.
/// </summary>
public class LoginResult
{
    /// <summary>
    /// Gets or sets the JWT access token.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the refresh token (returned only on login; clients must store it securely).
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the absolute expiration time of the access token (UTC).
    /// </summary>
    public DateTime AccessTokenExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the absolute expiration time of the refresh token (UTC).
    /// </summary>
    public DateTime RefreshTokenExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the unique session identifier associated with this login.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Gets or sets information about the authenticated user.
    /// </summary>
    public AuthenticatedUserInfo User { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether a 2FA challenge is required to complete login.
    /// When true, <see cref="AccessToken"/> and <see cref="RefreshToken"/> are empty; the client
    /// must complete the 2FA flow using <see cref="TwoFactorUserId"/>.
    /// </summary>
    public bool RequiresTwoFactor { get; set; }

    /// <summary>
    /// Gets or sets the user ID to pass to the 2FA verification endpoint when
    /// <see cref="RequiresTwoFactor"/> is true.
    /// </summary>
    public Guid TwoFactorUserId { get; set; }

    /// <summary>
    /// Gets or sets the remember-device token issued after successful 2FA verification when requested.
    /// Clients should store this securely and send it with future login attempts.
    /// </summary>
    public string? TwoFactorRememberDeviceToken { get; set; }
}

/// <summary>
/// Minimal user information returned on a successful login.
/// </summary>
public class AuthenticatedUserInfo
{
    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the roles assigned to the user.
    /// </summary>
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets a value indicating whether the user's email has been verified.
    /// </summary>
    public bool EmailVerified { get; set; }
}
