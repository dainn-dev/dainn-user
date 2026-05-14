namespace DainnStripe.Enums;

/// <summary>
/// Processing status for a persisted Stripe webhook event.
/// </summary>
public enum StripeWebhookProcessingStatus
{
    /// <summary>
    /// The event was received and persisted.
    /// </summary>
    Received = 0,

    /// <summary>
    /// The event is currently being processed.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// The event was processed successfully.
    /// </summary>
    Processed = 2,

    /// <summary>
    /// Event processing failed.
    /// </summary>
    Failed = 3
}
