namespace DainnUser.Infrastructure.Configuration;

/// <summary>
/// Configuration options for JWT token generation and validation.
/// </summary>
public class JwtOptions
{
    /// <summary>
    /// Gets or sets the secret key used to sign and validate JWT tokens.
    /// Must be at least 32 characters (256 bits) for HMAC-SHA256.
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JWT issuer claim ("iss").
    /// </summary>
    public string Issuer { get; set; } = "DainnUser";

    /// <summary>
    /// Gets or sets the JWT audience claim ("aud").
    /// </summary>
    public string Audience { get; set; } = "DainnUser";

    /// <summary>
    /// Gets or sets a value indicating whether to validate the JWT issuer.
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to validate the JWT audience.
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Gets or sets the clock skew in seconds applied during token validation.
    /// </summary>
    public int ClockSkewSeconds { get; set; } = 30;
}
