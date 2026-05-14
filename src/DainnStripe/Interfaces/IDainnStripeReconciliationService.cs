using DainnStripe.Models;

namespace DainnStripe.Interfaces;

/// <summary>
/// Reconciles local marketplace money movement records with Stripe.
/// </summary>
public interface IDainnStripeReconciliationService
{
    /// <summary>
    /// Reconciles transfers, payouts, and balance snapshots.
    /// </summary>
    Task<ReconcileMoneyMovementResult> ReconcileMoneyMovementAsync(
        ReconcileMoneyMovementRequest request,
        CancellationToken cancellationToken = default);
}
