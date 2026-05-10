using DainnUser.Core.Models.Authentication;

namespace DainnUser.Api.DTOs.Authentication;

/// <summary>
/// API response for initiating two-factor authentication setup.
/// </summary>
public class TwoFactorSetupResponse
{
    /// <summary>
    /// Gets or sets the shared TOTP secret.
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the otpauth URI for authenticator apps.
    /// </summary>
    public string OtpAuthUri { get; set; } = string.Empty;

    /// <summary>
    /// Maps a domain <see cref="TwoFactorSetupResult"/> into an API response.
    /// </summary>
    public static TwoFactorSetupResponse FromResult(TwoFactorSetupResult result)
    {
        return new TwoFactorSetupResponse
        {
            Secret = result.Secret,
            OtpAuthUri = result.OtpAuthUri
        };
    }
}
