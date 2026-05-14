namespace DainnStripe.Enums;

/// <summary>
/// Local payment status.
/// </summary>
public enum DainnStripePaymentStatus
{
    /// <summary>
    /// Payment was initiated.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Payment succeeded.
    /// </summary>
    Succeeded = 1,

    /// <summary>
    /// Payment failed.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Payment was canceled.
    /// </summary>
    Canceled = 3,

    /// <summary>
    /// Payment was refunded.
    /// </summary>
    Refunded = 4
}
