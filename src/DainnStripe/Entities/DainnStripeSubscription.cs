using DainnStripe.Enums;

namespace DainnStripe.Entities;

/// <summary>
/// Local subscription record linked to Stripe subscription data.
/// </summary>
public class DainnStripeSubscription
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
    /// Gets or sets the Stripe subscription ID.
    /// </summary>
    public string StripeSubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe customer ID.
    /// </summary>
    public string StripeCustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe price ID for the primary subscription item.
    /// </summary>
    public string? StripePriceId { get; set; }

    /// <summary>
    /// Gets or sets the local subscription status.
    /// </summary>
    public DainnStripeSubscriptionStatus Status { get; set; } = DainnStripeSubscriptionStatus.Unknown;

    /// <summary>
    /// Gets or sets a value indicating whether cancel-at-period-end is enabled.
    /// </summary>
    public bool CancelAtPeriodEnd { get; set; }

    /// <summary>
    /// Gets or sets the current period start.
    /// </summary>
    public DateTime? CurrentPeriodStart { get; set; }

    /// <summary>
    /// Gets or sets the current period end.
    /// </summary>
    public DateTime? CurrentPeriodEnd { get; set; }

    /// <summary>
    /// Gets or sets the cancellation timestamp.
    /// </summary>
    public DateTime? CanceledAt { get; set; }

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
