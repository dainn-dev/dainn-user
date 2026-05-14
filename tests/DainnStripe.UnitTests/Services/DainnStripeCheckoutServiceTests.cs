using DainnStripe.Data;
using DainnStripe.Enums;
using DainnStripe.Interfaces;
using DainnStripe.Models;
using DainnStripe.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DainnStripe.UnitTests.Services;

public class DainnStripeCheckoutServiceTests
{
    [Fact]
    public async Task CreateAsync_PaymentCheckout_PersistsPendingPayment()
    {
        await using var fixture = await CreateFixtureAsync();
        var client = new FakeCheckoutSessionClient(new CheckoutSessionResult
        {
            SessionId = "cs_test_1",
            Url = "https://checkout.stripe.test/session",
            StripeCustomerId = "cus_1",
            StripePaymentIntentId = "pi_1"
        });
        var service = new DainnStripeCheckoutService(client, fixture.DbContext);

        var result = await service.CreateAsync(new CreateCheckoutSessionRequest
        {
            OwnerId = "user_1",
            Mode = DainnStripeCheckoutMode.Payment,
            SuccessUrl = "https://app.test/success",
            CancelUrl = "https://app.test/cancel"
        }.WithLineItem("price_1", 2, 1500, "USD"));

        result.SessionId.Should().Be("cs_test_1");

        var payment = await fixture.DbContext.DainnStripePayments.SingleAsync();
        payment.OwnerId.Should().Be("user_1");
        payment.StripeCheckoutSessionId.Should().Be("cs_test_1");
        payment.StripePaymentIntentId.Should().Be("pi_1");
        payment.StripeCustomerId.Should().Be("cus_1");
        payment.Amount.Should().Be(3000);
        payment.Currency.Should().Be("usd");
        payment.Status.Should().Be(DainnStripePaymentStatus.Pending);
    }

    [Fact]
    public async Task CreateAsync_SubscriptionCheckoutWithSubscriptionId_PersistsIncompleteSubscription()
    {
        await using var fixture = await CreateFixtureAsync();
        var client = new FakeCheckoutSessionClient(new CheckoutSessionResult
        {
            SessionId = "cs_sub_1",
            Url = "https://checkout.stripe.test/session",
            StripeCustomerId = "cus_1",
            StripeSubscriptionId = "sub_1"
        });
        var service = new DainnStripeCheckoutService(client, fixture.DbContext);

        await service.CreateAsync(new CreateCheckoutSessionRequest
        {
            OwnerId = "user_1",
            Mode = DainnStripeCheckoutMode.Subscription,
            SuccessUrl = "https://app.test/success",
            CancelUrl = "https://app.test/cancel"
        }.WithLineItem("price_monthly", 1));

        var subscription = await fixture.DbContext.DainnStripeSubscriptions.SingleAsync();
        subscription.OwnerId.Should().Be("user_1");
        subscription.StripeSubscriptionId.Should().Be("sub_1");
        subscription.StripeCustomerId.Should().Be("cus_1");
        subscription.StripePriceId.Should().Be("price_monthly");
        subscription.Status.Should().Be(DainnStripeSubscriptionStatus.Incomplete);
    }

    [Fact]
    public async Task CreateAsync_WithoutLineItems_Throws()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new DainnStripeCheckoutService(
            new FakeCheckoutSessionClient(new CheckoutSessionResult()),
            fixture.DbContext);

        var request = new CreateCheckoutSessionRequest
        {
            OwnerId = "user_1",
            Mode = DainnStripeCheckoutMode.Payment,
            SuccessUrl = "https://app.test/success",
            CancelUrl = "https://app.test/cancel"
        };

        await service.Invoking(item => item.CreateAsync(request))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("At least one checkout line item is required.*");
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

    private sealed class FakeCheckoutSessionClient : IDainnStripeCheckoutSessionClient
    {
        private readonly CheckoutSessionResult _result;

        public FakeCheckoutSessionClient(CheckoutSessionResult result)
        {
            _result = result;
        }

        public Task<CheckoutSessionResult> CreateAsync(
            CreateCheckoutSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }
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

internal static class CheckoutSessionRequestTestExtensions
{
    public static CreateCheckoutSessionRequest WithLineItem(
        this CreateCheckoutSessionRequest request,
        string priceId,
        long quantity,
        long? unitAmount = null,
        string? currency = null)
    {
        request.LineItems.Add(new CreateCheckoutSessionLineItem
        {
            StripePriceId = priceId,
            Quantity = quantity,
            UnitAmount = unitAmount,
            Currency = currency
        });

        return request;
    }
}
