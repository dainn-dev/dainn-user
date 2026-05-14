using DainnStripe.Models;

namespace DainnStripe.Interfaces;

/// <summary>
/// Retrieves Stripe balance snapshots behind a testable abstraction.
/// </summary>
public interface IDainnStripeBalanceClient
{
    /// <summary>
    /// Retrieves the current balance snapshot.
    /// </summary>
    Task<BalanceSnapshotResult> GetAsync(
        string? stripeAccountId = null,
        CancellationToken cancellationToken = default);
}
