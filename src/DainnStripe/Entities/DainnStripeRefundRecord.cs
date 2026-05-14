using DainnStripe.Enums;

namespace DainnStripe.Entities;

/// <summary>
/// Records a Stripe refund issued against a local payment.
/// </summary>
public class DainnStripeRefundRecord
{
    /// <summary>
    /// Gets or sets the row identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the parent payment identifier.
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Gets or sets the Stripe Refund ID.
    /// </summary>
    public string StripeRefundId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe Charge ID.
    /// </summary>
    public string? StripeChargeId { get; set; }

    /// <summary>
    /// Gets or sets the Stripe PaymentIntent ID.
    /// </summary>
    public string? StripePaymentIntentId { get; set; }

    /// <summary>
    /// Gets or sets the refund amount in the smallest currency unit.
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency.
    /// </summary>
    public string Currency { get; set; } = "usd";

    /// <summary>
    /// Gets or sets the refund reason.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the refund status.
    /// </summary>
    public DainnStripeRefundStatus Status { get; set; }

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

    /// <summary>
    /// Gets or sets the parent payment navigation property.
    /// </summary>
    public DainnStripePayment Payment { get; set; } = null!;
}
