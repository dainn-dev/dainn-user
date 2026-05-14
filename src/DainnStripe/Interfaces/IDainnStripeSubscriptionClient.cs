using DainnStripe.Models;

namespace DainnStripe.Interfaces;

/// <summary>
/// Low-level Stripe API client for subscription operations.
/// </summary>
public interface IDainnStripeSubscriptionClient
{
    /// <summary>
    /// Creates a Stripe subscription and returns the result.
    /// </summary>
    Task<SubscriptionResult> CreateAsync(
        CreateSubscriptionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Immediately cancels a Stripe subscription and returns the updated result.
    /// </summary>
    Task<SubscriptionResult> CancelAsync(
        string stripeSubscriptionId,
        CancellationToken cancellationToken = default);
}
