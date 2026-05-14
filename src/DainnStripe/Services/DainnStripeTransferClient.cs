using DainnStripe.Interfaces;
using DainnStripe.Models;
using Stripe;

namespace DainnStripe.Services;

/// <summary>
/// Stripe.net backed Transfer client.
/// </summary>
public class DainnStripeTransferClient : IDainnStripeTransferClient
{
    private readonly TransferService _transferService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeTransferClient"/> class.
    /// </summary>
    public DainnStripeTransferClient()
        : this(new TransferService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeTransferClient"/> class.
    /// </summary>
    public DainnStripeTransferClient(TransferService transferService)
    {
        _transferService = transferService;
    }

    /// <inheritdoc />
    public async Task<TransferResult> CreateAsync(
        CreateTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var options = new TransferCreateOptions
        {
            Amount = request.Amount,
            Currency = request.Currency,
            Destination = request.StripeDestinationAccountId,
            Description = request.Description,
            SourceTransaction = request.StripeSourceTransactionId,
            TransferGroup = request.TransferGroup,
            Metadata = request.Metadata.Count == 0
                ? null
                : new Dictionary<string, string>(request.Metadata, StringComparer.Ordinal)
        };

        var requestOptions = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? null
            : new RequestOptions { IdempotencyKey = request.IdempotencyKey };

        var transfer = await _transferService.CreateAsync(options, requestOptions, cancellationToken);

        return new TransferResult
        {
            TransferId = transfer.Id,
            DestinationAccountId = transfer.DestinationId ?? request.StripeDestinationAccountId,
            SourceTransactionId = transfer.SourceTransactionId ?? request.StripeSourceTransactionId,
            Amount = transfer.Amount,
            Currency = transfer.Currency,
            TransferGroup = transfer.TransferGroup,
            Reversed = transfer.Reversed
        };
    }

    /// <inheritdoc />
    public async Task<TransferResult> GetAsync(
        string transferId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transferId);

        var transfer = await _transferService.GetAsync(transferId, null, null, cancellationToken);

        return new TransferResult
        {
            TransferId = transfer.Id,
            DestinationAccountId = transfer.DestinationId ?? string.Empty,
            SourceTransactionId = transfer.SourceTransactionId,
            Amount = transfer.Amount,
            Currency = transfer.Currency,
            TransferGroup = transfer.TransferGroup,
            Reversed = transfer.Reversed
        };
    }
}
