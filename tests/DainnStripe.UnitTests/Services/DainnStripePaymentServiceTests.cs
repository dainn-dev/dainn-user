using DainnStripe.Data;
using DainnStripe.Interfaces;
using DainnStripe.Models;
using DainnStripe.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DainnStripe.UnitTests.Services;

public class DainnStripePaymentServiceTests
{
    [Fact]
    public async Task CreatePaymentIntentAsync_PersistsPendingPayment()
    {
        await using var fixture = await CreateFixtureAsync();
        var client = new FakePaymentIntentClient(new PaymentIntentResult
        {
            PaymentIntentId = "pi_1",
            ClientSecret = "pi_1_secret",
            StripeCustomerId = "cus_1",
            Amount = 2500,
            Currency = "USD",
            Status = "requires_payment_method"
        });
        var service = new DainnStripePaymentService(client, fixture.DbContext);

        var result = await service.CreatePaymentIntentAsync(new CreatePaymentIntentRequest
        {
            OwnerId = "user_1",
            Amount = 2500,
            Currency = "usd",
            StripeCustomerId = "cus_1"
        });

        result.PaymentIntentId.Should().Be("pi_1");

        var payment = await fixture.DbContext.DainnStripePayments.SingleAsync();
        payment.OwnerId.Should().Be("user_1");
        payment.StripePaymentIntentId.Should().Be("pi_1");
        payment.StripeCustomerId.Should().Be("cus_1");
        payment.Amount.Should().Be(2500);
        payment.Currency.Should().Be("usd");
    }

    [Fact]
    public async Task CreatePaymentIntentAsync_InvalidAmount_Throws()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new DainnStripePaymentService(
            new FakePaymentIntentClient(new PaymentIntentResult()),
            fixture.DbContext);

        var request = new CreatePaymentIntentRequest
        {
            OwnerId = "user_1",
            Amount = 0,
            Currency = "usd"
        };

        await service.Invoking(item => item.CreatePaymentIntentAsync(request))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("Amount must be greater than zero.*");
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

    private sealed class FakePaymentIntentClient : IDainnStripePaymentIntentClient
    {
        private readonly PaymentIntentResult _result;

        public FakePaymentIntentClient(PaymentIntentResult result)
        {
            _result = result;
        }

        public Task<PaymentIntentResult> CreateAsync(
            CreatePaymentIntentRequest request,
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
