namespace DainnUser.Application.DTOs.Authentication;

/// <summary>
/// Data transfer object for operations that require a two-factor authentication code.
/// </summary>
public class TwoFactorCodeDto
{
    /// <summary>
    /// Gets or sets the TOTP or backup code.
    /// </summary>
    public string Code { get; set; } = string.Empty;
}
