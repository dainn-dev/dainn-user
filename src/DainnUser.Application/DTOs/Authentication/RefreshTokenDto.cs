namespace DainnUser.Application.DTOs.Authentication;

/// <summary>
/// Data transfer object for refreshing a JWT access token.
/// </summary>
public class RefreshTokenDto
{
    /// <summary>
    /// Gets or sets the refresh token issued at login (or by a previous refresh).
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}
