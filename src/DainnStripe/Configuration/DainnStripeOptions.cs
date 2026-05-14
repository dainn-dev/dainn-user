namespace DainnStripe.Configuration;

/// <summary>
/// Configuration options for the DainnStripe foundation module.
/// </summary>
public class DainnStripeOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether Stripe integration is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the Stripe secret API key.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe publishable key.
    /// </summary>
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the webhook endpoint signing secret.
    /// </summary>
    public string WebhookSigningSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the allowed timestamp tolerance for webhook signature validation.
    /// </summary>
    public long WebhookToleranceSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets a value indicating whether Stripe.net should reject events with an API version mismatch.
    /// </summary>
    public bool ThrowOnApiVersionMismatch { get; set; } = false;

    /// <summary>
    /// Gets or sets the default webhook route mapped by the endpoint extension.
    /// </summary>
    public string WebhookPath { get; set; } = "/stripe/webhooks";
}
