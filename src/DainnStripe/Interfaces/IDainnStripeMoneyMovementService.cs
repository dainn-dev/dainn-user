using DainnStripe.Models;

namespace DainnStripe.Interfaces;

/// <summary>
/// Orchestrates marketplace transfers and payouts.
/// </summary>
public interface IDainnStripeMoneyMovementService
{
    /// <summary>
    /// Creates a transfer to a connected account and stores the local record.
    /// </summary>
    Task<TransferResult> CreateTransferAsync(
        CreateTransferRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a payout for a connected account and stores the local record.
    /// </summary>
    Task<PayoutResult> CreatePayoutAsync(
        CreatePayoutRequest request,
        CancellationToken cancellationToken = default);
}
