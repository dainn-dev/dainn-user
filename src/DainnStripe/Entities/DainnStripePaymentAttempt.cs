using DainnStripe.Enums;

namespace DainnStripe.Entities;

/// <summary>
/// Records a single charge attempt for a local payment.
/// </summary>
public class DainnStripePaymentAttempt
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
    /// Gets or sets the Stripe PaymentIntent ID.
    /// </summary>
    public string? StripePaymentIntentId { get; set; }

    /// <summary>
    /// Gets or sets the Stripe Charge ID.
    /// </summary>
    public string? StripeChargeId { get; set; }

    /// <summary>
    /// Gets or sets the attempt status.
    /// </summary>
    public DainnStripePaymentAttemptStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the attempted amount in the smallest currency unit.
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency.
    /// </summary>
    public string Currency { get; set; } = "usd";

    /// <summary>
    /// Gets or sets the Stripe error code when the attempt failed.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the Stripe error message when the attempt failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the parent payment navigation property.
    /// </summary>
    public DainnStripePayment Payment { get; set; } = null!;
}
