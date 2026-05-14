using System.Text.Json;
using DainnStripe.Data;
using DainnStripe.Entities;
using DainnStripe.Enums;
using DainnStripe.Interfaces;
using DainnStripe.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace DainnStripe.Services;

/// <summary>
/// Finalizes local subscription and invoice state from Stripe subscription webhook events.
/// </summary>
public class DainnStripeSubscriptionWebhookHandler : IStripeWebhookHandler
{
    private static readonly HashSet<string> SupportedEventTypes = new(StringComparer.Ordinal)
    {
        "customer.subscription.created",
        "customer.subscription.updated",
        "customer.subscription.deleted",
        "invoice.payment_succeeded",
        "invoice.payment_failed",
        "invoice.finalized"
    };

    private readonly DainnStripeDbContext _dbContext;
    private readonly IDainnStripeSubscriptionService _subscriptionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeSubscriptionWebhookHandler"/> class.
    /// </summary>
    public DainnStripeSubscriptionWebhookHandler(
        DainnStripeDbContext dbContext,
        IDainnStripeSubscriptionService subscriptionService)
    {
        _dbContext = dbContext;
        _subscriptionService = subscriptionService;
    }

    /// <inheritdoc />
    public bool CanHandle(string eventType) => SupportedEventTypes.Contains(eventType);

    /// <inheritdoc />
    public async Task HandleAsync(
        Event stripeEvent,
        StripeWebhookEventRecord record,
        CancellationToken cancellationToken = default)
    {
        if (record.EventType.StartsWith("customer.subscription.", StringComparison.Ordinal))
        {
            await HandleSubscriptionEventAsync(record, cancellationToken);
        }
        else if (record.EventType.StartsWith("invoice.", StringComparison.Ordinal))
        {
            await HandleInvoiceEventAsync(record, cancellationToken);
        }
    }

    private async Task HandleSubscriptionEventAsync(
        StripeWebhookEventRecord record,
        CancellationToken cancellationToken)
    {
        var sub = ParseSubscriptionFromPayload(record.Payload);
        if (sub is null)
        {
            return;
        }

        var ownerId = await ResolveOwnerIdAsync(sub.StripeCustomerId, sub.StripeSubscriptionId, cancellationToken);

        await _subscriptionService.SyncAsync(new SyncSubscriptionRequest
        {
            OwnerId = ownerId,
            StripeSubscriptionId = sub.StripeSubscriptionId,
            StripeCustomerId = sub.StripeCustomerId,
            StripePriceId = sub.StripePriceId,
            Status = sub.Status,
            CancelAtPeriodEnd = sub.CancelAtPeriodEnd,
            CurrentPeriodStart = sub.CurrentPeriodStart,
            CurrentPeriodEnd = sub.CurrentPeriodEnd,
            CanceledAt = sub.CanceledAt
        }, cancellationToken);
    }

    private async Task HandleInvoiceEventAsync(
        StripeWebhookEventRecord record,
        CancellationToken cancellationToken)
    {
        var inv = ParseInvoiceFromPayload(record.Payload, record.EventType);
        if (inv is null)
        {
            return;
        }

        var ownerId = await ResolveOwnerIdAsync(inv.StripeCustomerId, null, cancellationToken);

        var existing = await _dbContext.DainnStripeInvoices
            .SingleOrDefaultAsync(i => i.StripeInvoiceId == inv.StripeInvoiceId, cancellationToken);

        var now = DateTime.UtcNow;

        if (existing is null)
        {
            _dbContext.DainnStripeInvoices.Add(new DainnStripeInvoice
            {
                Id = Guid.NewGuid(),
                StripeInvoiceId = inv.StripeInvoiceId,
                StripeSubscriptionId = inv.StripeSubscriptionId,
                OwnerId = ownerId,
                StripeCustomerId = inv.StripeCustomerId,
                AmountDue = inv.AmountDue,
                AmountPaid = inv.AmountPaid,
                Currency = inv.Currency,
                Status = inv.Status,
                HostedInvoiceUrl = inv.HostedInvoiceUrl,
                InvoicePdfUrl = inv.InvoicePdfUrl,
                PeriodStart = inv.PeriodStart,
                PeriodEnd = inv.PeriodEnd,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            existing.AmountDue = inv.AmountDue;
            existing.AmountPaid = inv.AmountPaid;
            existing.Status = inv.Status;
            existing.HostedInvoiceUrl = inv.HostedInvoiceUrl;
            existing.InvoicePdfUrl = inv.InvoicePdfUrl;
            existing.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> ResolveOwnerIdAsync(
        string stripeCustomerId,
        string? stripeSubscriptionId,
        CancellationToken cancellationToken)
    {
        // Prefer existing local subscription owner, then fall back to customer mapping.
        if (!string.IsNullOrWhiteSpace(stripeSubscriptionId))
        {
            var existingSub = await _dbContext.DainnStripeSubscriptions
                .AsNoTracking()
                .SingleOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, cancellationToken);

            if (existingSub is not null && !string.IsNullOrWhiteSpace(existingSub.OwnerId))
            {
                return existingSub.OwnerId;
            }
        }

        var mapping = await _dbContext.StripeCustomerMappings
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.StripeCustomerId == stripeCustomerId, cancellationToken);

        return mapping?.OwnerId ?? stripeCustomerId;
    }

    private static SubscriptionWebhookReference? ParseSubscriptionFromPayload(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        if (!document.RootElement.TryGetProperty("data", out var data)
            || !data.TryGetProperty("object", out var obj))
        {
            return null;
        }

        var id = ReadString(obj, "id");
        var customerId = ReadString(obj, "customer");
        if (id is null || customerId is null)
        {
            return null;
        }

        string? priceId = null;
        if (obj.TryGetProperty("items", out var items)
            && items.TryGetProperty("data", out var itemsData)
            && itemsData.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in itemsData.EnumerateArray())
            {
                if (item.TryGetProperty("price", out var price))
                {
                    priceId = ReadString(price, "id");
                }
                break;
            }
        }

        return new SubscriptionWebhookReference(
            id,
            customerId,
            priceId,
            MapSubscriptionStatus(ReadString(obj, "status")),
            ReadBool(obj, "cancel_at_period_end"),
            ReadUnixTimestamp(obj, "current_period_start"),
            ReadUnixTimestamp(obj, "current_period_end"),
            ReadUnixTimestamp(obj, "canceled_at"));
    }

    private static InvoiceWebhookReference? ParseInvoiceFromPayload(string payload, string eventType)
    {
        using var document = JsonDocument.Parse(payload);
        if (!document.RootElement.TryGetProperty("data", out var data)
            || !data.TryGetProperty("object", out var obj))
        {
            return null;
        }

        var id = ReadString(obj, "id");
        var customerId = ReadString(obj, "customer");
        if (id is null || customerId is null)
        {
            return null;
        }

        var status = eventType switch
        {
            "invoice.payment_succeeded" => DainnStripeInvoiceStatus.Paid,
            "invoice.payment_failed" => DainnStripeInvoiceStatus.Open,
            "invoice.finalized" => DainnStripeInvoiceStatus.Open,
            _ => DainnStripeInvoiceStatus.Open
        };

        return new InvoiceWebhookReference(
            id,
            ReadString(obj, "subscription"),
            customerId,
            ReadInt64(obj, "amount_due") ?? 0,
            ReadInt64(obj, "amount_paid") ?? 0,
            ReadString(obj, "currency")?.ToLowerInvariant() ?? "usd",
            status,
            ReadString(obj, "hosted_invoice_url"),
            ReadString(obj, "invoice_pdf"),
            ReadUnixTimestamp(obj, "period_start"),
            ReadUnixTimestamp(obj, "period_end"));
    }

    private static DainnStripeSubscriptionStatus MapSubscriptionStatus(string? status) => status switch
    {
        "active" => DainnStripeSubscriptionStatus.Active,
        "trialing" => DainnStripeSubscriptionStatus.Trialing,
        "past_due" => DainnStripeSubscriptionStatus.PastDue,
        "canceled" => DainnStripeSubscriptionStatus.Canceled,
        "unpaid" => DainnStripeSubscriptionStatus.Unpaid,
        "incomplete" => DainnStripeSubscriptionStatus.Incomplete,
        _ => DainnStripeSubscriptionStatus.Unknown
    };

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

    private static bool ReadBool(JsonElement obj, string propertyName)
    {
        if (!obj.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        return property.ValueKind == JsonValueKind.True;
    }

    private static DateTime? ReadUnixTimestamp(JsonElement obj, string propertyName)
    {
        var value = ReadInt64(obj, propertyName);
        return value.HasValue ? DateTimeOffset.FromUnixTimeSeconds(value.Value).UtcDateTime : null;
    }

    private sealed record SubscriptionWebhookReference(
        string StripeSubscriptionId,
        string StripeCustomerId,
        string? StripePriceId,
        DainnStripeSubscriptionStatus Status,
        bool CancelAtPeriodEnd,
        DateTime? CurrentPeriodStart,
        DateTime? CurrentPeriodEnd,
        DateTime? CanceledAt);

    private sealed record InvoiceWebhookReference(
        string StripeInvoiceId,
        string? StripeSubscriptionId,
        string StripeCustomerId,
        long AmountDue,
        long AmountPaid,
        string Currency,
        DainnStripeInvoiceStatus Status,
        string? HostedInvoiceUrl,
        string? InvoicePdfUrl,
        DateTime? PeriodStart,
        DateTime? PeriodEnd);
}
