using DainnStripe.Entities;
using DainnStripe.Models;

namespace DainnStripe.Interfaces;

/// <summary>
/// Synchronizes local subscription state from Stripe data.
/// </summary>
public interface IDainnStripeSubscriptionService
{
    /// <summary>
    /// Creates or updates a local subscription by Stripe subscription ID.
    /// </summary>
    Task<DainnStripeSubscription> SyncAsync(
        SyncSubscriptionRequest request,
        CancellationToken cancellationToken = default);
}
