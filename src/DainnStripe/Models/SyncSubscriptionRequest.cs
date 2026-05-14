using DainnStripe.Enums;

namespace DainnStripe.Models;

/// <summary>
/// Request to upsert local subscription state from Stripe.
/// </summary>
public sealed class SyncSubscriptionRequest
{
    /// <summary>
    /// Gets or sets the owner/user identifier.
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe subscription ID.
    /// </summary>
    public string StripeSubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe customer ID.
    /// </summary>
    public string StripeCustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe price ID.
    /// </summary>
    public string? StripePriceId { get; set; }

    /// <summary>
    /// Gets or sets subscription status.
    /// </summary>
    public DainnStripeSubscriptionStatus Status { get; set; } = DainnStripeSubscriptionStatus.Unknown;

    /// <summary>
    /// Gets or sets cancel-at-period-end.
    /// </summary>
    public bool CancelAtPeriodEnd { get; set; }

    /// <summary>
    /// Gets or sets current period start.
    /// </summary>
    public DateTime? CurrentPeriodStart { get; set; }

    /// <summary>
    /// Gets or sets current period end.
    /// </summary>
    public DateTime? CurrentPeriodEnd { get; set; }

    /// <summary>
    /// Gets or sets cancellation timestamp.
    /// </summary>
    public DateTime? CanceledAt { get; set; }

    /// <summary>
    /// Gets or sets serialized metadata.
    /// </summary>
    public string? MetadataJson { get; set; }
}
