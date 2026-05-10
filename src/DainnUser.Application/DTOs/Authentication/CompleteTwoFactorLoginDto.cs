namespace DainnUser.Application.DTOs.Authentication;

/// <summary>
/// Data transfer object for completing a two-factor login challenge.
/// </summary>
public class CompleteTwoFactorLoginDto
{
    /// <summary>
    /// Gets or sets the user ID returned by the login challenge response.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the TOTP or backup code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to trust this device for future logins.
    /// </summary>
    public bool RememberDevice { get; set; }
}
