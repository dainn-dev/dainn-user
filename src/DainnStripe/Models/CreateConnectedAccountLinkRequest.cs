namespace DainnStripe.Models;

/// <summary>
/// Request to create a Stripe connected account onboarding link.
/// </summary>
public sealed class CreateConnectedAccountLinkRequest
{
    /// <summary>
    /// Gets or sets the Stripe connected account ID.
    /// </summary>
    public string StripeAccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL Stripe returns users to after onboarding.
    /// </summary>
    public string ReturnUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL Stripe redirects users to when the link expires.
    /// </summary>
    public string RefreshUrl { get; set; } = string.Empty;
}
