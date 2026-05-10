namespace DainnUser.Core.Models.Authentication;

/// <summary>
/// Result returned when preparing TOTP authenticator setup for a user.
/// </summary>
public class TwoFactorSetupResult
{
    /// <summary>
    /// Gets or sets the Base32-encoded TOTP secret to enter into an authenticator app.
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the otpauth URI consumed by authenticator apps and QR renderers.
    /// </summary>
    public string OtpAuthUri { get; set; } = string.Empty;
}
