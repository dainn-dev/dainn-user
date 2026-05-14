using System.Text.Json;
using DainnStripe.Data;
using DainnStripe.Entities;
using DainnStripe.Interfaces;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace DainnStripe.Services;

/// <summary>
/// Persists balance snapshots from Stripe balance webhooks.
/// </summary>
public class DainnStripeBalanceWebhookHandler : IStripeWebhookHandler
{
    private readonly DainnStripeDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeBalanceWebhookHandler"/> class.
    /// </summary>
    public DainnStripeBalanceWebhookHandler(DainnStripeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public bool CanHandle(string eventType) => eventType == "balance.available";

    /// <inheritdoc />
    public async Task HandleAsync(
        Event stripeEvent,
        StripeWebhookEventRecord record,
        CancellationToken cancellationToken = default)
    {
        var balanceRef = BalanceWebhookReference.FromPayload(record.Payload, record.StripeAccountId, record.Livemode);
        if (balanceRef is null)
        {
            return;
        }

        var connectedAccount = string.IsNullOrWhiteSpace(balanceRef.StripeAccountId)
            ? null
            : await _dbContext.DainnStripeConnectedAccounts
                .SingleOrDefaultAsync(
                    item => item.StripeAccountId == balanceRef.StripeAccountId,
                    cancellationToken);

        var now = DateTime.UtcNow;
        _dbContext.DainnStripeBalanceSnapshots.Add(new DainnStripeBalanceSnapshot
        {
            Id = Guid.NewGuid(),
            ConnectedAccountId = connectedAccount?.Id,
            StripeAccountId = balanceRef.StripeAccountId,
            AvailableJson = balanceRef.AvailableJson,
            PendingJson = balanceRef.PendingJson,
            Livemode = balanceRef.Livemode,
            CreatedAt = now,
            UpdatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record BalanceWebhookReference(
        string? StripeAccountId,
        string AvailableJson,
        string PendingJson,
        bool Livemode)
    {
        public static BalanceWebhookReference? FromPayload(
            string payload,
            string? recordStripeAccountId,
            bool recordLivemode)
        {
            using var document = JsonDocument.Parse(payload);
            if (!document.RootElement.TryGetProperty("data", out var data)
                || !data.TryGetProperty("object", out var obj))
            {
                return null;
            }

            var availableJson = ReadRawJson(obj, "available") ?? "[]";
            var pendingJson = ReadRawJson(obj, "pending") ?? "[]";

            return new BalanceWebhookReference(
                recordStripeAccountId ?? ReadString(document.RootElement, "account"),
                availableJson,
                pendingJson,
                ReadBoolean(document.RootElement, "livemode") ?? recordLivemode);
        }

        private static string? ReadRawJson(JsonElement obj, string propertyName)
        {
            return obj.TryGetProperty(propertyName, out var property)
                ? property.GetRawText()
                : null;
        }

        private static string? ReadString(JsonElement obj, string propertyName)
        {
            return obj.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
        }

        private static bool? ReadBoolean(JsonElement obj, string propertyName)
        {
            return obj.TryGetProperty(propertyName, out var property)
                ? property.ValueKind == JsonValueKind.True
                : null;
        }
    }
}
