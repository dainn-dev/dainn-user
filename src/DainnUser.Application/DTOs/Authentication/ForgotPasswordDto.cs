namespace DainnUser.Application.DTOs.Authentication;

/// <summary>
/// Data transfer object for initiating a password reset.
/// </summary>
public class ForgotPasswordDto
{
    /// <summary>
    /// Gets or sets the email address of the account for which a reset is requested.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}
