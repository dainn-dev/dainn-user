using DainnStripe.Models;

namespace DainnStripe.Interfaces;

/// <summary>
/// Creates Stripe Transfers behind a testable abstraction.
/// </summary>
public interface IDainnStripeTransferClient
{
    /// <summary>
    /// Creates a Stripe Transfer.
    /// </summary>
    Task<TransferResult> CreateAsync(
        CreateTransferRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a Stripe Transfer.
    /// </summary>
    Task<TransferResult> GetAsync(
        string transferId,
        CancellationToken cancellationToken = default);
}
