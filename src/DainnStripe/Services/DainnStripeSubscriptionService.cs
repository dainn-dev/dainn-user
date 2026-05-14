using DainnStripe.Data;
using DainnStripe.Entities;
using DainnStripe.Interfaces;
using DainnStripe.Models;
using Microsoft.EntityFrameworkCore;

namespace DainnStripe.Services;

/// <summary>
/// Default subscription synchronization service.
/// </summary>
public class DainnStripeSubscriptionService : IDainnStripeSubscriptionService
{
    private readonly DainnStripeDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeSubscriptionService"/> class.
    /// </summary>
    public DainnStripeSubscriptionService(DainnStripeDbContext dbContext)
    {
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

    private static void EnsureRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
