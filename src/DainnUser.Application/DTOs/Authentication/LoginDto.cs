namespace DainnUser.Application.DTOs.Authentication;

/// <summary>
/// Data transfer object for user login.
/// </summary>
public class LoginDto
{
    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's password.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional remember-device token from a previous 2FA verification.
    /// When valid, the 2FA challenge is skipped for this login.
    /// </summary>
    public string? RememberDeviceToken { get; set; }
}
