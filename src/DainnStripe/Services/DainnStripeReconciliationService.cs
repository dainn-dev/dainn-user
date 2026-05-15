using DainnStripe.Data;
using DainnStripe.Entities;
using DainnStripe.Interfaces;
using DainnStripe.Models;
using Microsoft.EntityFrameworkCore;

namespace DainnStripe.Services;

/// <summary>
/// Default reconciliation service for marketplace money movement records.
/// </summary>
public class DainnStripeReconciliationService : IDainnStripeReconciliationService
{
    private readonly IDainnStripeTransferClient _transferClient;
    private readonly IDainnStripePayoutClient _payoutClient;
    private readonly IDainnStripeBalanceClient _balanceClient;
    private readonly DainnStripeDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeReconciliationService"/> class.
    /// </summary>
    public DainnStripeReconciliationService(
        IDainnStripeTransferClient transferClient,
        IDainnStripePayoutClient payoutClient,
        IDainnStripeBalanceClient balanceClient,
        DainnStripeDbContext dbContext)
    {
        _transferClient = transferClient;
        _payoutClient = payoutClient;
        _balanceClient = balanceClient;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<ReconcileMoneyMovementResult> ReconcileMoneyMovementAsync(
        ReconcileMoneyMovementRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = new ReconcileMoneyMovementResult();

        if (request.IncludeTransfers)
        {
            result.TransfersReconciled = await ReconcileTransfersAsync(request, cancellationToken);
        }

        if (request.IncludePayouts)
        {
            result.PayoutsReconciled = await ReconcilePayoutsAsync(request, cancellationToken);
        }

        if (request.CaptureBalanceSnapshot)
        {
            result.BalanceSnapshotsCaptured = await CaptureBalanceSnapshotsAsync(request, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task<int> ReconcileTransfersAsync(
        ReconcileMoneyMovementRequest request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.DainnStripeTransfers.AsQueryable();
        query = ApplyTransferFilters(query, request);

        var transfers = await query.ToListAsync(cancellationToken);
        foreach (var transfer in transfers)
        {
            var stripeTransfer = await _transferClient.GetAsync(transfer.StripeTransferId, cancellationToken);
            transfer.Amount = stripeTransfer.Amount == 0 ? transfer.Amount : stripeTransfer.Amount;
            transfer.Currency = string.IsNullOrWhiteSpace(stripeTransfer.Currency)
                ? transfer.Currency
                : stripeTransfer.Currency.ToLowerInvariant();
            transfer.StripeDestinationAccountId = string.IsNullOrWhiteSpace(stripeTransfer.DestinationAccountId)
                ? transfer.StripeDestinationAccountId
                : stripeTransfer.DestinationAccountId;
            transfer.StripeSourceTransactionId = stripeTransfer.SourceTransactionId ?? transfer.StripeSourceTransactionId;
            transfer.TransferGroup = stripeTransfer.TransferGroup ?? transfer.TransferGroup;
            transfer.Reversed = stripeTransfer.Reversed;
            transfer.UpdatedAt = DateTime.UtcNow;
        }

        return transfers.Count;
    }

    private async Task<int> ReconcilePayoutsAsync(
        ReconcileMoneyMovementRequest request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.DainnStripePayouts.AsQueryable();
        query = ApplyPayoutFilters(query, request);

        var payouts = await query.ToListAsync(cancellationToken);
        foreach (var payout in payouts)
        {
            var stripePayout = await _payoutClient.GetAsync(
                payout.StripePayoutId,
                payout.StripeAccountId,
                cancellationToken);

            payout.Amount = stripePayout.Amount == 0 ? payout.Amount : stripePayout.Amount;
            payout.Currency = string.IsNullOrWhiteSpace(stripePayout.Currency)
                ? payout.Currency
                : stripePayout.Currency.ToLowerInvariant();
            payout.Destination = stripePayout.Destination ?? payout.Destination;
            payout.Method = stripePayout.Method ?? payout.Method;
            payout.Status = string.IsNullOrWhiteSpace(stripePayout.Status) ? payout.Status : stripePayout.Status;
            payout.ArrivalDate = stripePayout.ArrivalDate ?? payout.ArrivalDate;
            payout.UpdatedAt = DateTime.UtcNow;
        }

        return payouts.Count;
    }

    private async Task<int> CaptureBalanceSnapshotsAsync(
        ReconcileMoneyMovementRequest request,
        CancellationToken cancellationToken)
    {
        var accountIds = await ResolveBalanceAccountIdsAsync(request, cancellationToken);

        // Pre-fetch all connected accounts in a single query to avoid N+1
        var nonNullIds = accountIds.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id!).ToList();
        var connectedAccountMap = nonNullIds.Count == 0
            ? new Dictionary<string, DainnStripeConnectedAccount>()
            : await _dbContext.DainnStripeConnectedAccounts
                .Where(item => nonNullIds.Contains(item.StripeAccountId))
                .ToDictionaryAsync(item => item.StripeAccountId, cancellationToken);

        var count = 0;

        foreach (var accountId in accountIds)
        {
            var snapshot = await _balanceClient.GetAsync(accountId, cancellationToken);
            connectedAccountMap.TryGetValue(snapshot.StripeAccountId ?? string.Empty, out var connectedAccount);
            var now = DateTime.UtcNow;

            _dbContext.DainnStripeBalanceSnapshots.Add(new DainnStripeBalanceSnapshot
            {
                Id = Guid.NewGuid(),
                ConnectedAccountId = connectedAccount?.Id,
                StripeAccountId = snapshot.StripeAccountId,
                AvailableJson = snapshot.AvailableJson,
                PendingJson = snapshot.PendingJson,
                Livemode = snapshot.Livemode,
                CreatedAt = now,
                UpdatedAt = now
            });
            count++;

        }

        return count;
    }

    private async Task<IReadOnlyList<string?>> ResolveBalanceAccountIdsAsync(
        ReconcileMoneyMovementRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.StripeAccountId))
        {
            return new[] { request.StripeAccountId };
        }

        if (!string.IsNullOrWhiteSpace(request.OwnerId))
        {
            return await _dbContext.DainnStripeConnectedAccounts
                .Where(account => account.OwnerId == request.OwnerId)
                .Select(account => (string?)account.StripeAccountId)
                .ToListAsync(cancellationToken);
        }

        return new string?[] { null };
    }

    private static IQueryable<DainnStripeTransfer> ApplyTransferFilters(
        IQueryable<DainnStripeTransfer> query,
        ReconcileMoneyMovementRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.OwnerId))
        {
            query = query.Where(transfer => transfer.OwnerId == request.OwnerId);
        }

        if (!string.IsNullOrWhiteSpace(request.StripeAccountId))
        {
            query = query.Where(transfer => transfer.StripeDestinationAccountId == request.StripeAccountId);
        }

        return query;
    }

    private static IQueryable<DainnStripePayout> ApplyPayoutFilters(
        IQueryable<DainnStripePayout> query,
        ReconcileMoneyMovementRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.OwnerId))
        {
            query = query.Where(payout => payout.OwnerId == request.OwnerId);
        }

        if (!string.IsNullOrWhiteSpace(request.StripeAccountId))
        {
            query = query.Where(payout => payout.StripeAccountId == request.StripeAccountId);
        }

        return query;
    }
}
