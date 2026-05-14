namespace DainnStripe.Models;

/// <summary>
/// Result from creating a Stripe connected account link.
/// </summary>
public sealed class ConnectedAccountLinkResult
{
    /// <summary>
    /// Gets or sets the onboarding URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the URL expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}
