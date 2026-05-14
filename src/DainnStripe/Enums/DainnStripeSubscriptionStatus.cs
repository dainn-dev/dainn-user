namespace DainnStripe.Enums;

/// <summary>
/// Local subscription status.
/// </summary>
public enum DainnStripeSubscriptionStatus
{
    /// <summary>
    /// Subscription is incomplete.
    /// </summary>
    Incomplete = 0,

    /// <summary>
    /// Subscription is trialing.
    /// </summary>
    Trialing = 1,

    /// <summary>
    /// Subscription is active.
    /// </summary>
    Active = 2,

    /// <summary>
    /// Subscription payment is past due.
    /// </summary>
    PastDue = 3,

    /// <summary>
    /// Subscription is canceled.
    /// </summary>
    Canceled = 4,

    /// <summary>
    /// Subscription is unpaid.
    /// </summary>
    Unpaid = 5,

    /// <summary>
    /// Subscription status is unknown to this module.
    /// </summary>
    Unknown = 99
}
