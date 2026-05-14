using DainnStripe.Data;
using DainnStripe.Entities;
using DainnStripe.Interfaces;
using DainnStripe.Models;
using Microsoft.EntityFrameworkCore;

namespace DainnStripe.Services;

/// <summary>
/// Default marketplace money movement service.
/// </summary>
public class DainnStripeMoneyMovementService : IDainnStripeMoneyMovementService
{
    private readonly IDainnStripeTransferClient _transferClient;
    private readonly IDainnStripePayoutClient _payoutClient;
    private readonly DainnStripeDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeMoneyMovementService"/> class.
    /// </summary>
    public DainnStripeMoneyMovementService(
        IDainnStripeTransferClient transferClient,
        IDainnStripePayoutClient payoutClient,
        DainnStripeDbContext dbContext)
    {
        _transferClient = transferClient;
        _payoutClient = payoutClient;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<TransferResult> CreateTransferAsync(
        CreateTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureRequired(request.OwnerId, nameof(request.OwnerId));
        EnsureRequired(request.StripeDestinationAccountId, nameof(request.StripeDestinationAccountId));
        EnsureRequired(request.Currency, nameof(request.Currency));
        EnsurePositiveAmount(request.Amount, nameof(request.Amount));

        var account = await _dbContext.DainnStripeConnectedAccounts
            .SingleOrDefaultAsync(
                item => item.StripeAccountId == request.StripeDestinationAccountId
                    && item.OwnerId == request.OwnerId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"DainnStripe connected account '{request.StripeDestinationAccountId}' does not exist for owner '{request.OwnerId}'.");

        var result = await _transferClient.CreateAsync(request, cancellationToken);
        var now = DateTime.UtcNow;

        _dbContext.DainnStripeTransfers.Add(new DainnStripeTransfer
        {
            Id = Guid.NewGuid(),
            ConnectedAccountId = account.Id,
            OwnerId = request.OwnerId,
            StripeTransferId = result.TransferId,
            StripeDestinationAccountId = result.DestinationAccountId,
            StripeSourceTransactionId = result.SourceTransactionId,
            Amount = result.Amount == 0 ? request.Amount : result.Amount,
            Currency = string.IsNullOrWhiteSpace(result.Currency)
                ? request.Currency.ToLowerInvariant()
                : result.Currency.ToLowerInvariant(),
            TransferGroup = result.TransferGroup ?? request.TransferGroup,
            Reversed = result.Reversed,
            MetadataJson = TryGetMetadataJson(request.Metadata),
            CreatedAt = now,
            UpdatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    /// <inheritdoc />
    public async Task<PayoutResult> CreatePayoutAsync(
        CreatePayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureRequired(request.OwnerId, nameof(request.OwnerId));
        EnsureRequired(request.StripeAccountId, nameof(request.StripeAccountId));
        EnsureRequired(request.Currency, nameof(request.Currency));
        EnsurePositiveAmount(request.Amount, nameof(request.Amount));

        var account = await _dbContext.DainnStripeConnectedAccounts
            .SingleOrDefaultAsync(
                item => item.StripeAccountId == request.StripeAccountId
                    && item.OwnerId == request.OwnerId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"DainnStripe connected account '{request.StripeAccountId}' does not exist for owner '{request.OwnerId}'.");

        var result = await _payoutClient.CreateAsync(request, cancellationToken);
        var now = DateTime.UtcNow;

        _dbContext.DainnStripePayouts.Add(new DainnStripePayout
        {
            Id = Guid.NewGuid(),
            ConnectedAccountId = account.Id,
            OwnerId = request.OwnerId,
            StripePayoutId = result.PayoutId,
            StripeAccountId = result.StripeAccountId,
            Amount = result.Amount == 0 ? request.Amount : result.Amount,
            Currency = string.IsNullOrWhiteSpace(result.Currency)
                ? request.Currency.ToLowerInvariant()
                : result.Currency.ToLowerInvariant(),
            Destination = result.Destination ?? request.Destination,
            Method = result.Method ?? request.Method,
            Status = string.IsNullOrWhiteSpace(result.Status) ? "pending" : result.Status,
            ArrivalDate = result.ArrivalDate,
            MetadataJson = TryGetMetadataJson(request.Metadata),
            CreatedAt = now,
            UpdatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    private static string? TryGetMetadataJson(IDictionary<string, string> metadata)
    {
        return metadata.TryGetValue("metadata_json", out var metadataJson)
            ? metadataJson
            : null;
    }

    private static void EnsurePositiveAmount(long amount, string parameterName)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Amount must be greater than zero.");
        }
    }

    private static void EnsureRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
