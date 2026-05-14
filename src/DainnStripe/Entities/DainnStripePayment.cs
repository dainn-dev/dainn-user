using DainnStripe.Enums;

namespace DainnStripe.Entities;

/// <summary>
/// Local payment record linked to Stripe Checkout/PaymentIntent data.
/// </summary>
public class DainnStripePayment
{
    /// <summary>
    /// Gets or sets the row identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the application owner identifier.
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe checkout session ID.
    /// </summary>
    public string? StripeCheckoutSessionId { get; set; }

    /// <summary>
    /// Gets or sets the Stripe payment intent ID.
    /// </summary>
    public string? StripePaymentIntentId { get; set; }

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
    /// Gets or sets the local payment status.
    /// </summary>
    public DainnStripePaymentStatus Status { get; set; } = DainnStripePaymentStatus.Pending;

    /// <summary>
    /// Gets or sets serialized metadata.
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
