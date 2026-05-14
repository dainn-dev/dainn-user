namespace DainnStripe.Enums;

/// <summary>
/// Local refund status.
/// </summary>
public enum DainnStripeRefundStatus
{
    /// <summary>Refund is pending.</summary>
    Pending = 0,

    /// <summary>Refund succeeded.</summary>
    Succeeded = 1,

    /// <summary>Refund failed.</summary>
    Failed = 2,

    /// <summary>Refund was canceled.</summary>
    Canceled = 3
}
