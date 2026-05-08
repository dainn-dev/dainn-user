namespace DainnUser.Application.DTOs.Authentication;

/// <summary>
/// Data transfer object for user registration.
/// </summary>
public class RegisterDto
{
    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password confirmation.
    /// </summary>
    public string ConfirmPassword { get; set; } = string.Empty;
}
