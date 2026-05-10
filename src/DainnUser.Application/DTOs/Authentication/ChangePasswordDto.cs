namespace DainnUser.Application.DTOs.Authentication;

/// <summary>
/// Data transfer object for changing an authenticated user's password.
/// </summary>
public class ChangePasswordDto
{
    /// <summary>
    /// Gets or sets the user's current (existing) password for verification.
    /// </summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new password to set.
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the confirmation of the new password (must match <see cref="NewPassword"/>).
    /// </summary>
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
