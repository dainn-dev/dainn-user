using DainnStripe.Entities;

namespace DainnStripe.Models;

/// <summary>
/// Result returned after processing a Stripe webhook payload.
/// </summary>
public sealed class StripeWebhookProcessResult
{
    /// <summary>
    /// Gets or sets the persisted event record.
    /// </summary>
    public StripeWebhookEventRecord EventRecord { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the event had already been processed.
    /// </summary>
    public bool IsDuplicate { get; set; }

    /// <summary>
    /// Gets or sets the number of handlers that processed the event.
    /// </summary>
    public int HandlerCount { get; set; }
}
