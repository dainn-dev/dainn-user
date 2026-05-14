using System.Text.Json;
using DainnStripe.Entities;
using DainnStripe.Enums;
using DainnStripe.Interfaces;
using DainnStripe.Models;
using Stripe;

namespace DainnStripe.Services;

/// <summary>
/// Synchronizes local connected account capability state from Stripe account webhooks.
/// </summary>
public class DainnStripeConnectAccountWebhookHandler : IStripeWebhookHandler
{
    private readonly IDainnStripeConnectService _connectService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeConnectAccountWebhookHandler"/> class.
    /// </summary>
    public DainnStripeConnectAccountWebhookHandler(IDainnStripeConnectService connectService)
    {
        _connectService = connectService;
    }

    /// <inheritdoc />
    public bool CanHandle(string eventType)
    {
        return eventType == "account.updated";
    }

    /// <inheritdoc />
    public async Task HandleAsync(
        Event stripeEvent,
        StripeWebhookEventRecord record,
        CancellationToken cancellationToken = default)
    {
        var accountState = AccountWebhookState.FromPayload(record.Payload);
        if (accountState is null)
        {
            return;
        }

        await _connectService.SyncConnectedAccountAsync(
            new SyncConnectedAccountRequest
            {
                StripeAccountId = accountState.StripeAccountId,
                ChargesEnabled = accountState.ChargesEnabled,
                PayoutsEnabled = accountState.PayoutsEnabled,
                DetailsSubmitted = accountState.DetailsSubmitted,
                Status = accountState.Disabled
                    ? DainnStripeConnectedAccountStatus.Disabled
                    : null
            },
            cancellationToken);
    }

    private sealed record AccountWebhookState(
        string StripeAccountId,
        bool ChargesEnabled,
        bool PayoutsEnabled,
        bool DetailsSubmitted,
        bool Disabled)
    {
        public static AccountWebhookState? FromPayload(string payload)
        {
            using var document = JsonDocument.Parse(payload);
            if (!document.RootElement.TryGetProperty("data", out var data)
                || !data.TryGetProperty("object", out var obj))
            {
                return null;
            }

            var accountId = ReadString(obj, "id");
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return null;
            }

            return new AccountWebhookState(
                accountId,
                ReadBoolean(obj, "charges_enabled"),
                ReadBoolean(obj, "payouts_enabled"),
                ReadBoolean(obj, "details_submitted"),
                IsDisabled(obj));
        }

        private static bool IsDisabled(JsonElement obj)
        {
            if (!obj.TryGetProperty("requirements", out var requirements)
                || !requirements.TryGetProperty("disabled_reason", out var disabledReason)
                || disabledReason.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(disabledReason.GetString());
        }

        private static string? ReadString(JsonElement obj, string propertyName)
        {
            return obj.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
        }

        private static bool ReadBoolean(JsonElement obj, string propertyName)
        {
            return obj.TryGetProperty(propertyName, out var property)
                && property.ValueKind == JsonValueKind.True;
        }
    }
}
