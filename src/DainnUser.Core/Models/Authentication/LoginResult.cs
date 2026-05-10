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
