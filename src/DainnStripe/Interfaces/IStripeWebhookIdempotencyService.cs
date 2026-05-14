using DainnStripe.Entities;
using Stripe;

namespace DainnStripe.Interfaces;

/// <summary>
/// Provides idempotent webhook inbox operations for Stripe events.
/// </summary>
public interface IStripeWebhookIdempotencyService
{
    /// <summary>
    /// Gets an existing webhook record by Stripe event ID, or creates a new inbox record.
    /// </summary>
    Task<StripeWebhookEventRecord> GetOrCreateAsync(
        Event stripeEvent,
        string payload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when the event was already processed successfully.
    /// </summary>
    bool IsProcessed(StripeWebhookEventRecord record);

    /// <summary>
    /// Marks a webhook record as processing and increments its attempt counter.
    /// </summary>
    Task MarkProcessingAsync(
        StripeWebhookEventRecord record,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a webhook record as processed.
    /// </summary>
    Task MarkProcessedAsync(
        StripeWebhookEventRecord record,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a webhook record as failed.
    /// </summary>
    Task MarkFailedAsync(
        StripeWebhookEventRecord record,
        string errorMessage,
        CancellationToken cancellationToken = default);
}
