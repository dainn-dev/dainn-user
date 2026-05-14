using DainnStripe.Enums;

namespace DainnStripe.Entities;

/// <summary>
/// Inbox record used to persist Stripe webhook events and enforce idempotency.
/// </summary>
public class StripeWebhookEventRecord
{
    /// <summary>
    /// Gets or sets the row identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the Stripe event identifier.
    /// </summary>
    public string StripeEventId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe event type.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe API version attached to the event.
    /// </summary>
    public string? ApiVersion { get; set; }

    /// <summary>
    /// Gets or sets the Stripe account identifier for Connect events.
    /// </summary>
    public string? StripeAccountId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the event was created in live mode.
    /// </summary>
    public bool Livemode { get; set; }

    /// <summary>
    /// Gets or sets the raw webhook JSON payload.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the processing status.
    /// </summary>
    public StripeWebhookProcessingStatus Status { get; set; } = StripeWebhookProcessingStatus.Received;

    /// <summary>
    /// Gets or sets the number of processing attempts.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Gets or sets the latest processing error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets when the record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the event was processed successfully.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Gets or sets when the record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
