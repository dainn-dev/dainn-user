using DainnStripe.Entities;
using DainnStripe.Models;

namespace DainnStripe.Interfaces;

/// <summary>
/// Manages local subscription state and orchestrates Stripe subscription operations.
/// </summary>
public interface IDainnStripeSubscriptionService
{
    /// <summary>
    /// Creates or updates a local subscription by Stripe subscription ID.
    /// </summary>
    Task<DainnStripeSubscription> SyncAsync(
        SyncSubscriptionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Stripe subscription via API and persists it locally.
    /// </summary>
    Task<DainnStripeSubscription> CreateAsync(
        CreateSubscriptionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Immediately cancels a Stripe subscription and marks it canceled locally.
    /// </summary>
    Task<DainnStripeSubscription> CancelAsync(
        string ownerId,
        string stripeSubscriptionId,
        CancellationToken cancellationToken = default);
}
