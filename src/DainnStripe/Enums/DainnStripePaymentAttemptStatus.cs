namespace DainnStripe.Enums;

/// <summary>
/// Status of a single payment attempt.
/// </summary>
public enum DainnStripePaymentAttemptStatus
{
    /// <summary>Attempt is pending.</summary>
    Pending = 0,

    /// <summary>Attempt succeeded.</summary>
    Succeeded = 1,

    /// <summary>Attempt failed.</summary>
    Failed = 2
}
