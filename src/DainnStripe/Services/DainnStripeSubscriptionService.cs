using DainnStripe.Data;
using DainnStripe.Entities;
using DainnStripe.Enums;
using DainnStripe.Interfaces;
using DainnStripe.Models;
using Microsoft.EntityFrameworkCore;

namespace DainnStripe.Services;

/// <summary>
/// Manages subscription creation, cancellation, and local sync.
/// </summary>
public class DainnStripeSubscriptionService : IDainnStripeSubscriptionService
{
    private readonly IDainnStripeSubscriptionClient _subscriptionClient;
    private readonly DainnStripeDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeSubscriptionService"/> class.
    /// </summary>
    public DainnStripeSubscriptionService(
        IDainnStripeSubscriptionClient subscriptionClient,
        DainnStripeDbContext dbContext)
    {
        _subscriptionClient = subscriptionClient;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<DainnStripeSubscription> SyncAsync(
        SyncSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureRequired(request.OwnerId, nameof(request.OwnerId));
        EnsureRequired(request.StripeSubscriptionId, nameof(request.StripeSubscriptionId));
        EnsureRequired(request.StripeCustomerId, nameof(request.StripeCustomerId));

        var subscription = await _dbContext.DainnStripeSubscriptions
            .SingleOrDefaultAsync(
                item => item.StripeSubscriptionId == request.StripeSubscriptionId,
                cancellationToken);

        var now = DateTime.UtcNow;
        if (subscription is null)
        {
            subscription = new DainnStripeSubscription
            {
                Id = Guid.NewGuid(),
                StripeSubscriptionId = request.StripeSubscriptionId,
                CreatedAt = now
            };
            _dbContext.DainnStripeSubscriptions.Add(subscription);
        }

        subscription.OwnerId = request.OwnerId;
        subscription.StripeCustomerId = request.StripeCustomerId;
        subscription.StripePriceId = request.StripePriceId;
        subscription.Status = request.Status;
        subscription.CancelAtPeriodEnd = request.CancelAtPeriodEnd;
        subscription.CurrentPeriodStart = request.CurrentPeriodStart;
        subscription.CurrentPeriodEnd = request.CurrentPeriodEnd;
        subscription.CanceledAt = request.CanceledAt;
        subscription.MetadataJson = request.MetadataJson;
        subscription.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return subscription;
    }

    /// <inheritdoc />
    public async Task<DainnStripeSubscription> CreateAsync(
        CreateSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureRequired(request.OwnerId, nameof(request.OwnerId));
        EnsureRequired(request.StripeCustomerId, nameof(request.StripeCustomerId));
        EnsureRequired(request.StripePriceId, nameof(request.StripePriceId));

        var result = await _subscriptionClient.CreateAsync(request, cancellationToken);
        var now = DateTime.UtcNow;

        var subscription = new DainnStripeSubscription
        {
            Id = Guid.NewGuid(),
            OwnerId = request.OwnerId,
            StripeSubscriptionId = result.StripeSubscriptionId,
            StripeCustomerId = result.StripeCustomerId,
            StripePriceId = request.StripePriceId,
            Status = result.Status,
            CancelAtPeriodEnd = result.CancelAtPeriodEnd,
            CurrentPeriodEnd = result.CurrentPeriodEnd,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.DainnStripeSubscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return subscription;
    }

    /// <inheritdoc />
    public async Task<DainnStripeSubscription> CancelAsync(
        string ownerId,
        string stripeSubscriptionId,
        CancellationToken cancellationToken = default)
    {
        EnsureRequired(ownerId, nameof(ownerId));
        EnsureRequired(stripeSubscriptionId, nameof(stripeSubscriptionId));

        var result = await _subscriptionClient.CancelAsync(stripeSubscriptionId, cancellationToken);

        var subscription = await _dbContext.DainnStripeSubscriptions
            .SingleOrDefaultAsync(
                s => s.StripeSubscriptionId == stripeSubscriptionId && s.OwnerId == ownerId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"Subscription '{stripeSubscriptionId}' not found for owner '{ownerId}'.");

        subscription.Status = DainnStripeSubscriptionStatus.Canceled;
        subscription.CanceledAt = DateTime.UtcNow;
        subscription.CancelAtPeriodEnd = result.CancelAtPeriodEnd;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return subscription;
    }

    private static void EnsureRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
