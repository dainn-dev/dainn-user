namespace DainnStripe.Models;

/// <summary>
/// Result from creating a Stripe Checkout session.
/// </summary>
public sealed class CheckoutSessionResult
{
    /// <summary>
    /// Gets or sets the Stripe Checkout session ID.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hosted Checkout URL.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the Stripe customer ID.
    /// </summary>
    public string? StripeCustomerId { get; set; }

    /// <summary>
    /// Gets or sets the Stripe payment intent ID.
    /// </summary>
    public string? StripePaymentIntentId { get; set; }

    /// <summary>
    /// Gets or sets the Stripe subscription ID.
    /// </summary>
    public string? StripeSubscriptionId { get; set; }
}
