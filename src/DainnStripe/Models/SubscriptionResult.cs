using DainnStripe.Enums;

namespace DainnStripe.Models;

/// <summary>
/// Result of a Stripe subscription create or cancel operation.
/// </summary>
public sealed class SubscriptionResult
{
    /// <summary>
    /// Gets or sets the Stripe Subscription ID.
    /// </summary>
    public string StripeSubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe Customer ID.
    /// </summary>
    public string StripeCustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subscription status.
    /// </summary>
    public DainnStripeSubscriptionStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the end of the current billing period.
    /// </summary>
    public DateTime? CurrentPeriodEnd { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the subscription cancels at period end.
    /// </summary>
    public bool CancelAtPeriodEnd { get; set; }
}
