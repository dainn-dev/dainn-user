using System.Text.Json;
using DainnStripe.Data;
using DainnStripe.Entities;
using DainnStripe.Interfaces;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace DainnStripe.Services;

/// <summary>
/// Synchronizes local payout records from Stripe payout webhooks.
/// </summary>
public class DainnStripePayoutWebhookHandler : IStripeWebhookHandler
{
    private static readonly HashSet<string> SupportedEventTypes = new(StringComparer.Ordinal)
    {
        "payout.created",
        "payout.updated",
        "payout.paid",
        "payout.failed",
        "payout.canceled"
    };

    private readonly DainnStripeDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripePayoutWebhookHandler"/> class.
    /// </summary>
    public DainnStripePayoutWebhookHandler(DainnStripeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public bool CanHandle(string eventType) => SupportedEventTypes.Contains(eventType);

    /// <inheritdoc />
    public async Task HandleAsync(
        Event stripeEvent,
        StripeWebhookEventRecord record,
        CancellationToken cancellationToken = default)
    {
        var payoutRef = PayoutWebhookReference.FromPayload(record.Payload, record.StripeAccountId);
        if (payoutRef is null)
        {
            return;
        }

        var account = await _dbContext.DainnStripeConnectedAccounts
            .SingleOrDefaultAsync(item => item.StripeAccountId == payoutRef.StripeAccountId, cancellationToken);

        if (account is null)
        {
            return;
        }

        var payout = await _dbContext.DainnStripePayouts
            .SingleOrDefaultAsync(item => item.StripePayoutId == payoutRef.PayoutId, cancellationToken);

        var now = DateTime.UtcNow;
        if (payout is null)
        {
            payout = new DainnStripePayout
            {
                Id = Guid.NewGuid(),
                ConnectedAccountId = account.Id,
                OwnerId = account.OwnerId,
                StripePayoutId = payoutRef.PayoutId,
                StripeAccountId = payoutRef.StripeAccountId,
                CreatedAt = now
            };
            _dbContext.DainnStripePayouts.Add(payout);
        }

        payout.ConnectedAccountId = account.Id;
        payout.OwnerId = account.OwnerId;
        payout.StripeAccountId = payoutRef.StripeAccountId;
        payout.Amount = payoutRef.Amount;
        payout.Currency = payoutRef.Currency.ToLowerInvariant();
        payout.Destination = payoutRef.Destination;
        payout.Method = payoutRef.Method;
        payout.Status = payoutRef.Status;
        payout.ArrivalDate = payoutRef.ArrivalDate;
        payout.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record PayoutWebhookReference(
        string PayoutId,
        string StripeAccountId,
        long Amount,
        string Currency,
        string? Destination,
        string? Method,
        string Status,
        DateTime? ArrivalDate)
    {
        public static PayoutWebhookReference? FromPayload(string payload, string? recordStripeAccountId)
        {
            using var document = JsonDocument.Parse(payload);
            if (!document.RootElement.TryGetProperty("data", out var data)
                || !data.TryGetProperty("object", out var obj))
            {
                return null;
            }

            var payoutId = ReadString(obj, "id");
            var stripeAccountId = recordStripeAccountId ?? ReadString(document.RootElement, "account");
            var currency = ReadString(obj, "currency");
            var status = ReadString(obj, "status");

            if (string.IsNullOrWhiteSpace(payoutId)
                || string.IsNullOrWhiteSpace(stripeAccountId)
                || string.IsNullOrWhiteSpace(currency)
                || string.IsNullOrWhiteSpace(status))
            {
                return null;
            }

            return new PayoutWebhookReference(
                payoutId,
                stripeAccountId,
                ReadInt64(obj, "amount") ?? 0,
                currency,
                ReadString(obj, "destination"),
                ReadString(obj, "method"),
                status,
                ReadUnixTime(obj, "arrival_date"));
        }

        private static string? ReadString(JsonElement obj, string propertyName)
        {
            return obj.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
        }

        private static long? ReadInt64(JsonElement obj, string propertyName)
        {
            return obj.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number
                ? property.GetInt64()
                : null;
        }

        private static DateTime? ReadUnixTime(JsonElement obj, string propertyName)
        {
            var value = ReadInt64(obj, propertyName);
            return value.HasValue ? DateTimeOffset.FromUnixTimeSeconds(value.Value).UtcDateTime : null;
        }
    }
}
