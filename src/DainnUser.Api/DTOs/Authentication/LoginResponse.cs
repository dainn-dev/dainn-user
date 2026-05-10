using DainnUser.Core.Models.Authentication;

namespace DainnUser.Api.DTOs.Authentication;

/// <summary>
/// API response for a successful login.
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// Gets or sets the JWT access token.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the refresh token.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token type. Always "Bearer".
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Gets or sets the access-token lifetime in seconds.
    /// </summary>
    public int ExpiresIn { get; set; }

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
    /// Maps a domain <see cref="LoginResult"/> into an API <see cref="LoginResponse"/>.
    /// </summary>
    public static LoginResponse FromResult(LoginResult result)
    {
        return new LoginResponse
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresIn = (int)Math.Max(0, (result.AccessTokenExpiresAt - DateTime.UtcNow).TotalSeconds),
            AccessTokenExpiresAt = result.AccessTokenExpiresAt,
            RefreshTokenExpiresAt = result.RefreshTokenExpiresAt,
            SessionId = result.SessionId,
            User = result.User
        };
    }
}
