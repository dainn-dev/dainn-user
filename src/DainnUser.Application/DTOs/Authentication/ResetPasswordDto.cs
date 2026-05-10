namespace DainnUser.Application.DTOs.Authentication;

/// <summary>
/// Data transfer object for completing a password reset.
/// </summary>
public class ResetPasswordDto
{
    /// <summary>
    /// Gets or sets the password reset token previously delivered via email.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new password.
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the confirmation of the new password (must match <see cref="NewPassword"/>).
    /// </summary>
    public string ConfirmPassword { get; set; } = string.Empty;
}
