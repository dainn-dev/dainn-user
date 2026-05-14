using System.Security.Cryptography;
using System.Text;
using DainnStripe.Data;
using DainnStripe.Entities;
using DainnStripe.Enums;
using DainnStripe.Exceptions;
using DainnStripe.Interfaces;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace DainnStripe.Services;

/// <summary>
/// EF Core implementation of Stripe webhook idempotency.
/// </summary>
public class StripeWebhookIdempotencyService : IStripeWebhookIdempotencyService
{
    private readonly DainnStripeDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="StripeWebhookIdempotencyService"/> class.
    /// </summary>
    public StripeWebhookIdempotencyService(DainnStripeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<StripeWebhookEventRecord> GetOrCreateAsync(
        Event stripeEvent,
        string payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stripeEvent);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        var payloadHash = ComputePayloadHash(payload);

        var existing = await _dbContext.StripeWebhookEvents
            .FirstOrDefaultAsync(record => record.StripeEventId == stripeEvent.Id, cancellationToken);

        if (existing is not null)
        {
            if (existing.PayloadHash != payloadHash)
            {
                throw new StripeWebhookFingerprintConflictException(
                    stripeEvent.Id,
                    existing.PayloadHash,
                    payloadHash);
            }

            existing.UpdatedAt = DateTime.UtcNow;
            return existing;
        }

        var record = new StripeWebhookEventRecord
        {
            Id = Guid.NewGuid(),
            StripeEventId = stripeEvent.Id,
            EventType = stripeEvent.Type,
            ApiVersion = stripeEvent.ApiVersion,
            StripeAccountId = stripeEvent.Account,
            Livemode = stripeEvent.Livemode,
            Payload = payload,
            PayloadHash = payloadHash,
            Status = StripeWebhookProcessingStatus.Received,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.StripeWebhookEvents.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return record;
    }

    /// <inheritdoc />
    public bool IsProcessed(StripeWebhookEventRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return record.Status == StripeWebhookProcessingStatus.Processed;
    }

    /// <inheritdoc />
    public async Task MarkProcessingAsync(
        StripeWebhookEventRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        record.Status = StripeWebhookProcessingStatus.Processing;
        record.Attempts++;
        record.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkProcessedAsync(
        StripeWebhookEventRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        record.Status = StripeWebhookProcessingStatus.Processed;
        record.ProcessedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;
        record.ErrorMessage = null;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkFailedAsync(
        StripeWebhookEventRecord record,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        record.Status = StripeWebhookProcessingStatus.Failed;
        record.ErrorMessage = Truncate(errorMessage, 2048);
        record.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string ComputePayloadHash(string payload)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
