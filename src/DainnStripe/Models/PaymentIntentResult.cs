namespace DainnStripe.Models;

/// <summary>
/// Result from creating a Stripe PaymentIntent.
/// </summary>
public sealed class PaymentIntentResult
{
    /// <summary>
    /// Gets or sets the Stripe PaymentIntent ID.
    /// </summary>
    public string PaymentIntentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client secret returned by Stripe.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the Stripe customer ID.
    /// </summary>
    public string? StripeCustomerId { get; set; }

    /// <summary>
    /// Gets or sets the amount in the smallest currency unit.
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency.
    /// </summary>
    public string Currency { get; set; } = "usd";

    /// <summary>
    /// Gets or sets the Stripe status.
    /// </summary>
    public string? Status { get; set; }
}
