using DainnStripe.Entities;
using Stripe;

namespace DainnStripe.Interfaces;

/// <summary>
/// Handles a verified Stripe webhook event.
/// </summary>
public interface IStripeWebhookHandler
{
    /// <summary>
    /// Returns true when this handler should process the supplied Stripe event type.
    /// </summary>
    bool CanHandle(string eventType);

    /// <summary>
    /// Handles a verified Stripe webhook event.
    /// </summary>
    Task HandleAsync(Event stripeEvent, StripeWebhookEventRecord record, CancellationToken cancellationToken = default);
}
