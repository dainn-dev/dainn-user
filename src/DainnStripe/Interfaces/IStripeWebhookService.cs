using DainnStripe.Models;

namespace DainnStripe.Interfaces;

/// <summary>
/// Validates, persists, and dispatches Stripe webhook events.
/// </summary>
public interface IStripeWebhookService
{
    /// <summary>
    /// Processes a raw Stripe webhook payload.
    /// </summary>
    Task<StripeWebhookProcessResult> ProcessAsync(
        string payload,
        string signatureHeader,
        CancellationToken cancellationToken = default);
}
