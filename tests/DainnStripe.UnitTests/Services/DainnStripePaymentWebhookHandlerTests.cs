using DainnStripe.Data;
using DainnStripe.Entities;
using DainnStripe.Enums;
using DainnStripe.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace DainnStripe.UnitTests.Services;

public class DainnStripePaymentWebhookHandlerTests
{
    [Fact]
    public async Task HandleAsync_PaymentIntentSucceeded_MarksPaymentSucceeded()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.DbContext.DainnStripePayments.Add(new DainnStripePayment
        {
            Id = Guid.NewGuid(),
            OwnerId = "user_1",
            StripePaymentIntentId = "pi_1",
            Amount = 1000,
            Currency = "usd",
            Status = DainnStripePaymentStatus.Pending
        });
        await fixture.DbContext.SaveChangesAsync();

        var handler = new DainnStripePaymentWebhookHandler(fixture.DbContext);
        var record = new StripeWebhookEventRecord
        {
            StripeEventId = "evt_1",
            EventType = "payment_intent.succeeded",
            Payload = """
            {
              "data": {
                "object": {
                  "id": "pi_1",
                  "customer": "cus_1",
                  "amount_received": 1200,
                  "currency": "usd"
                }
              }
            }
            """
        };

        await handler.HandleAsync(new Event { Id = "evt_1", Type = record.EventType }, record);

        var payment = await fixture.DbContext.DainnStripePayments.SingleAsync();
        payment.Status.Should().Be(DainnStripePaymentStatus.Succeeded);
        payment.Amount.Should().Be(1200);
        payment.StripeCustomerId.Should().Be("cus_1");
    }

    [Fact]
    public async Task HandleAsync_CheckoutSessionExpired_MarksPaymentCanceled()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.DbContext.DainnStripePayments.Add(new DainnStripePayment
        {
            Id = Guid.NewGuid(),
            OwnerId = "user_1",
            StripeCheckoutSessionId = "cs_1",
            Amount = 1000,
            Currency = "usd",
            Status = DainnStripePaymentStatus.Pending
        });
        await fixture.DbContext.SaveChangesAsync();

        var handler = new DainnStripePaymentWebhookHandler(fixture.DbContext);
        var record = new StripeWebhookEventRecord
        {
            StripeEventId = "evt_1",
            EventType = "checkout.session.expired",
            Payload = """
            {
              "data": {
                "object": {
                  "id": "cs_1",
                  "payment_intent": "pi_1",
                  "customer": "cus_1",
                  "amount_total": 1000,
                  "currency": "usd"
                }
              }
            }
            """
        };

        await handler.HandleAsync(new Event { Id = "evt_1", Type = record.EventType }, record);

        var payment = await fixture.DbContext.DainnStripePayments.SingleAsync();
        payment.Status.Should().Be(DainnStripePaymentStatus.Canceled);
        payment.StripePaymentIntentId.Should().Be("pi_1");
    }

    [Fact]
    public async Task HandleAsync_ChargeRefunded_CreatesRefundRecord()
    {
        await using var fixture = await CreateFixtureAsync();
        var paymentId = Guid.NewGuid();
        fixture.DbContext.DainnStripePayments.Add(new DainnStripePayment
        {
            Id = paymentId,
            OwnerId = "user_1",
            StripePaymentIntentId = "pi_refund_1",
            Amount = 5000,
            Currency = "usd",
            Status = DainnStripePaymentStatus.Succeeded
        });
        await fixture.DbContext.SaveChangesAsync();

        var handler = new DainnStripePaymentWebhookHandler(fixture.DbContext);
        var record = new StripeWebhookEventRecord
        {
            StripeEventId = "evt_ref",
            EventType = "charge.refunded",
            Payload = """
            {
              "data": {
                "object": {
                  "id": "ch_1",
                  "payment_intent": "pi_refund_1",
                  "amount_refunded": 2000,
                  "currency": "usd",
                  "refunds": {
                    "data": [
                      {
                        "id": "re_1",
                        "charge": "ch_1",
                        "amount": 2000,
                        "currency": "usd",
                        "status": "succeeded",
                        "reason": "requested_by_customer"
                      }
                    ]
                  }
                }
              }
            }
            """
        };

        await handler.HandleAsync(new Event { Id = "evt_ref", Type = "charge.refunded" }, record);

        var refund = await fixture.DbContext.DainnStripeRefundRecords.SingleAsync();
        refund.StripeRefundId.Should().Be("re_1");
        refund.Amount.Should().Be(2000);
        refund.Status.Should().Be(DainnStripe.Enums.DainnStripeRefundStatus.Succeeded);
        refund.Reason.Should().Be("requested_by_customer");
        refund.PaymentId.Should().Be(paymentId);
    }

    [Fact]
    public async Task HandleAsync_ChargeRefunded_SkipsDuplicateRefundId()
    {
        await using var fixture = await CreateFixtureAsync();
        var paymentId = Guid.NewGuid();
        fixture.DbContext.DainnStripePayments.Add(new DainnStripePayment
        {
            Id = paymentId,
            OwnerId = "user_1",
            StripePaymentIntentId = "pi_dup_ref",
            Amount = 5000,
            Currency = "usd",
            Status = DainnStripePaymentStatus.Succeeded
        });
        await fixture.DbContext.SaveChangesAsync();

        var handler = new DainnStripePaymentWebhookHandler(fixture.DbContext);
        var record = new StripeWebhookEventRecord
        {
            StripeEventId = "evt_dup",
            EventType = "charge.refunded",
            Payload = """
            {
              "data": {
                "object": {
                  "id": "ch_dup",
                  "payment_intent": "pi_dup_ref",
                  "amount_refunded": 1000,
                  "currency": "usd",
                  "refunds": {
                    "data": [
                      { "id": "re_dup", "amount": 1000, "currency": "usd", "status": "succeeded" }
                    ]
                  }
                }
              }
            }
            """
        };

        await handler.HandleAsync(new Event { Id = "evt_dup", Type = "charge.refunded" }, record);
        await handler.HandleAsync(new Event { Id = "evt_dup", Type = "charge.refunded" }, record);

        (await fixture.DbContext.DainnStripeRefundRecords.CountAsync()).Should().Be(1);
    }

    private static async Task<TestFixture> CreateFixtureAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<DainnStripeDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new DainnStripeDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        return new TestFixture(connection, dbContext);
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public TestFixture(SqliteConnection connection, DainnStripeDbContext dbContext)
        {
            _connection = connection;
            DbContext = dbContext;
        }

        public DainnStripeDbContext DbContext { get; }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
