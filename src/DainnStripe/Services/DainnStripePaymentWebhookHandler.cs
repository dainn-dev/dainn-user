using System.Text.Json;
using DainnStripe.Data;
using DainnStripe.Entities;
using DainnStripe.Enums;
using DainnStripe.Interfaces;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace DainnStripe.Services;

/// <summary>
/// Finalizes local payments from Stripe payment webhook events.
/// </summary>
public class DainnStripePaymentWebhookHandler : IStripeWebhookHandler
{
    private static readonly HashSet<string> SupportedEventTypes = new(StringComparer.Ordinal)
    {
        "payment_intent.succeeded",
        "payment_intent.payment_failed",
        "payment_intent.canceled",
        "checkout.session.completed",
        "checkout.session.expired"
    };

    private readonly DainnStripeDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripePaymentWebhookHandler"/> class.
    /// </summary>
    public DainnStripePaymentWebhookHandler(DainnStripeDbContext dbContext)
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
        var paymentRef = PaymentWebhookReference.FromPayload(record.Payload, record.EventType);
        if (paymentRef is null)
        {
            return;
        }

        var payment = await FindPaymentAsync(paymentRef, cancellationToken);
        if (payment is null)
        {
            return;
        }

        payment.Status = paymentRef.Status;
        payment.UpdatedAt = DateTime.UtcNow;

        if (paymentRef.Amount.HasValue)
        {
            payment.Amount = paymentRef.Amount.Value;
        }

        if (!string.IsNullOrWhiteSpace(paymentRef.Currency))
        {
            payment.Currency = paymentRef.Currency.ToLowerInvariant();
        }

        if (!string.IsNullOrWhiteSpace(paymentRef.StripeCustomerId))
        {
            payment.StripeCustomerId = paymentRef.StripeCustomerId;
        }

        if (!string.IsNullOrWhiteSpace(paymentRef.StripePaymentIntentId))
        {
            payment.StripePaymentIntentId = paymentRef.StripePaymentIntentId;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<DainnStripePayment?> FindPaymentAsync(
        PaymentWebhookReference paymentRef,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(paymentRef.StripePaymentIntentId))
        {
            var byPaymentIntent = await _dbContext.DainnStripePayments
                .SingleOrDefaultAsync(
                    item => item.StripePaymentIntentId == paymentRef.StripePaymentIntentId,
                    cancellationToken);

            if (byPaymentIntent is not null)
            {
                return byPaymentIntent;
            }
        }

        if (!string.IsNullOrWhiteSpace(paymentRef.StripeCheckoutSessionId))
        {
            return await _dbContext.DainnStripePayments
                .SingleOrDefaultAsync(
                    item => item.StripeCheckoutSessionId == paymentRef.StripeCheckoutSessionId,
                    cancellationToken);
        }

        return null;
    }

    private sealed record PaymentWebhookReference(
        string? StripePaymentIntentId,
        string? StripeCheckoutSessionId,
        string? StripeCustomerId,
        long? Amount,
        string? Currency,
        DainnStripePaymentStatus Status)
    {
        public static PaymentWebhookReference? FromPayload(string payload, string eventType)
        {
            using var document = JsonDocument.Parse(payload);
            if (!document.RootElement.TryGetProperty("data", out var data)
                || !data.TryGetProperty("object", out var obj))
            {
                return null;
            }

            return eventType switch
            {
                "payment_intent.succeeded" => FromPaymentIntent(obj, DainnStripePaymentStatus.Succeeded),
                "payment_intent.payment_failed" => FromPaymentIntent(obj, DainnStripePaymentStatus.Failed),
                "payment_intent.canceled" => FromPaymentIntent(obj, DainnStripePaymentStatus.Canceled),
                "checkout.session.completed" => FromCheckoutSession(obj, DainnStripePaymentStatus.Succeeded),
                "checkout.session.expired" => FromCheckoutSession(obj, DainnStripePaymentStatus.Canceled),
                _ => null
            };
        }

        private static PaymentWebhookReference FromPaymentIntent(JsonElement obj, DainnStripePaymentStatus status)
        {
            return new PaymentWebhookReference(
                ReadString(obj, "id"),
                null,
                ReadString(obj, "customer"),
                ReadInt64(obj, status == DainnStripePaymentStatus.Succeeded ? "amount_received" : "amount"),
                ReadString(obj, "currency"),
                status);
        }

        private static PaymentWebhookReference FromCheckoutSession(JsonElement obj, DainnStripePaymentStatus status)
        {
            return new PaymentWebhookReference(
                ReadString(obj, "payment_intent"),
                ReadString(obj, "id"),
                ReadString(obj, "customer"),
                ReadInt64(obj, "amount_total"),
                ReadString(obj, "currency"),
                status);
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
    }
}
