using DainnStripe.Models;

namespace DainnStripe.Interfaces;

/// <summary>
/// Creates Stripe Payouts behind a testable abstraction.
/// </summary>
public interface IDainnStripePayoutClient
{
    /// <summary>
    /// Creates a Stripe Payout.
    /// </summary>
    Task<PayoutResult> CreateAsync(
        CreatePayoutRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a Stripe Payout.
    /// </summary>
    Task<PayoutResult> GetAsync(
        string payoutId,
        string stripeAccountId,
        CancellationToken cancellationToken = default);
}
