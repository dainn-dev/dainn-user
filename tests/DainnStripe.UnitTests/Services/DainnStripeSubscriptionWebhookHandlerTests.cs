using DainnStripe.Data;
using DainnStripe.Entities;
using DainnStripe.Enums;
using DainnStripe.Interfaces;
using DainnStripe.Models;
using DainnStripe.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace DainnStripe.UnitTests.Services;

public class DainnStripeSubscriptionWebhookHandlerTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DainnStripeDbContext _dbContext;
    private readonly DainnStripeSubscriptionWebhookHandler _handler;

    public DainnStripeSubscriptionWebhookHandlerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DainnStripeDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new DainnStripeDbContext(options);
        _dbContext.Database.EnsureCreated();

        var subscriptionService = new DainnStripeSubscriptionService(
            new FakeSubscriptionClient(),
            _dbContext);
        _handler = new DainnStripeSubscriptionWebhookHandler(_dbContext, subscriptionService);
    }

    [Fact]
    public void CanHandle_SubscriptionEvents_ReturnsTrue()
    {
        _handler.CanHandle("customer.subscription.created").Should().BeTrue();
        _handler.CanHandle("customer.subscription.updated").Should().BeTrue();
        _handler.CanHandle("customer.subscription.deleted").Should().BeTrue();
    }

    [Fact]
    public void CanHandle_InvoiceEvents_ReturnsTrue()
    {
        _handler.CanHandle("invoice.payment_succeeded").Should().BeTrue();
        _handler.CanHandle("invoice.payment_failed").Should().BeTrue();
        _handler.CanHandle("invoice.finalized").Should().BeTrue();
    }

    [Fact]
    public void CanHandle_UnrelatedEvent_ReturnsFalse()
    {
        _handler.CanHandle("payment_intent.succeeded").Should().BeFalse();
        _handler.CanHandle("charge.refunded").Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_SubscriptionCreated_CreatesLocalSubscription()
    {
        var record = BuildRecord("customer.subscription.created", BuildSubscriptionPayload(
            "sub_1", "cus_1", "price_1", "active",
            cancelAtPeriodEnd: false, periodEnd: 1893456000));

        await _handler.HandleAsync(new Event { Id = "evt_1", Type = record.EventType }, record);

        var sub = await _dbContext.DainnStripeSubscriptions.SingleAsync();
        sub.StripeSubscriptionId.Should().Be("sub_1");
        sub.StripeCustomerId.Should().Be("cus_1");
        sub.Status.Should().Be(DainnStripeSubscriptionStatus.Active);
    }

    [Fact]
    public async Task HandleAsync_SubscriptionDeleted_MarksCanceled()
    {
        _dbContext.DainnStripeSubscriptions.Add(new DainnStripeSubscription
        {
            Id = Guid.NewGuid(),
            OwnerId = "user_1",
            StripeSubscriptionId = "sub_del",
            StripeCustomerId = "cus_1",
            Status = DainnStripeSubscriptionStatus.Active
        });
        await _dbContext.SaveChangesAsync();

        var record = BuildRecord("customer.subscription.deleted", BuildSubscriptionPayload(
            "sub_del", "cus_1", "price_1", "canceled",
            cancelAtPeriodEnd: false, periodEnd: 1893456000));

        await _handler.HandleAsync(new Event { Id = "evt_2", Type = record.EventType }, record);

        var sub = await _dbContext.DainnStripeSubscriptions.SingleAsync();
        sub.Status.Should().Be(DainnStripeSubscriptionStatus.Canceled);
    }

    [Fact]
    public async Task HandleAsync_InvoicePaymentSucceeded_CreatesInvoiceRecord()
    {
        var record = BuildRecord("invoice.payment_succeeded", BuildInvoicePayload(
            "in_1", "sub_1", "cus_1", 2000, 2000, "usd", "paid"));

        await _handler.HandleAsync(new Event { Id = "evt_3", Type = record.EventType }, record);

        var invoice = await _dbContext.DainnStripeInvoices.SingleAsync();
        invoice.StripeInvoiceId.Should().Be("in_1");
        invoice.AmountPaid.Should().Be(2000);
        invoice.Status.Should().Be(DainnStripeInvoiceStatus.Paid);
    }

    [Fact]
    public async Task HandleAsync_InvoiceEventTwice_UpsertsRecord()
    {
        var record = BuildRecord("invoice.payment_succeeded", BuildInvoicePayload(
            "in_dup", "sub_1", "cus_1", 1000, 1000, "usd", "paid"));

        await _handler.HandleAsync(new Event { Id = "evt_4", Type = record.EventType }, record);
        await _handler.HandleAsync(new Event { Id = "evt_5", Type = record.EventType }, record);

        (await _dbContext.DainnStripeInvoices.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_InvoicePaymentFailed_SetsOpenStatus()
    {
        var record = BuildRecord("invoice.payment_failed", BuildInvoicePayload(
            "in_fail", "sub_1", "cus_1", 1500, 0, "usd", "open"));

        await _handler.HandleAsync(new Event { Id = "evt_6", Type = record.EventType }, record);

        var invoice = await _dbContext.DainnStripeInvoices.SingleAsync();
        invoice.Status.Should().Be(DainnStripeInvoiceStatus.Open);
        invoice.AmountPaid.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_SubscriptionCreated_ResolvesOwnerFromCustomerMapping()
    {
        _dbContext.StripeCustomerMappings.Add(new StripeCustomerMapping
        {
            Id = Guid.NewGuid(),
            OwnerId = "app_user_42",
            StripeCustomerId = "cus_mapped",
            Livemode = false
        });
        await _dbContext.SaveChangesAsync();

        var record = BuildRecord("customer.subscription.created", BuildSubscriptionPayload(
            "sub_mapped", "cus_mapped", "price_1", "active",
            cancelAtPeriodEnd: false, periodEnd: 1893456000));

        await _handler.HandleAsync(new Event { Id = "evt_7", Type = record.EventType }, record);

        var sub = await _dbContext.DainnStripeSubscriptions.SingleAsync(s => s.StripeSubscriptionId == "sub_mapped");
        sub.OwnerId.Should().Be("app_user_42");
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private static StripeWebhookEventRecord BuildRecord(string eventType, string payload) =>
        new()
        {
            StripeEventId = Guid.NewGuid().ToString(),
            EventType = eventType,
            Payload = payload,
            PayloadHash = "abc"
        };

    private static string BuildSubscriptionPayload(
        string subId, string customerId, string priceId, string status,
        bool cancelAtPeriodEnd, long periodEnd) => $$"""
        {
          "data": {
            "object": {
              "id": "{{subId}}",
              "customer": "{{customerId}}",
              "status": "{{status}}",
              "cancel_at_period_end": {{(cancelAtPeriodEnd ? "true" : "false")}},
              "current_period_start": 1893369600,
              "current_period_end": {{periodEnd}},
              "canceled_at": null,
              "items": {
                "data": [
                  { "price": { "id": "{{priceId}}" } }
                ]
              }
            }
          }
        }
        """;

    private static string BuildInvoicePayload(
        string invoiceId, string subId, string customerId,
        long amountDue, long amountPaid, string currency, string status) => $$"""
        {
          "data": {
            "object": {
              "id": "{{invoiceId}}",
              "subscription": "{{subId}}",
              "customer": "{{customerId}}",
              "amount_due": {{amountDue}},
              "amount_paid": {{amountPaid}},
              "currency": "{{currency}}",
              "status": "{{status}}",
              "hosted_invoice_url": null,
              "invoice_pdf": null,
              "period_start": 1893369600,
              "period_end": 1893456000
            }
          }
        }
        """;

    private sealed class FakeSubscriptionClient : IDainnStripeSubscriptionClient
    {
        public Task<SubscriptionResult> CreateAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<SubscriptionResult> CancelAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
