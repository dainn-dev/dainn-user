using DainnStripe.Data;
using DainnStripe.Enums;
using DainnStripe.Models;
using DainnStripe.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DainnStripe.UnitTests.Services;

public class DainnStripeSubscriptionServiceTests
{
    [Fact]
    public async Task SyncAsync_NewSubscription_PersistsState()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new DainnStripeSubscriptionService(fixture.DbContext);
        var periodEnd = DateTime.UtcNow.AddDays(30);

        var subscription = await service.SyncAsync(new SyncSubscriptionRequest
        {
            OwnerId = "user_1",
            StripeSubscriptionId = "sub_1",
            StripeCustomerId = "cus_1",
            StripePriceId = "price_1",
            Status = DainnStripeSubscriptionStatus.Active,
            CurrentPeriodEnd = periodEnd
        });

        subscription.Id.Should().NotBeEmpty();
        subscription.Status.Should().Be(DainnStripeSubscriptionStatus.Active);
        subscription.CurrentPeriodEnd.Should().Be(periodEnd);
        (await fixture.DbContext.DainnStripeSubscriptions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SyncAsync_ExistingSubscription_UpdatesState()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new DainnStripeSubscriptionService(fixture.DbContext);

        await service.SyncAsync(new SyncSubscriptionRequest
        {
            OwnerId = "user_1",
            StripeSubscriptionId = "sub_1",
            StripeCustomerId = "cus_1",
            StripePriceId = "price_1",
            Status = DainnStripeSubscriptionStatus.Active
        });

        var updated = await service.SyncAsync(new SyncSubscriptionRequest
        {
            OwnerId = "user_1",
            StripeSubscriptionId = "sub_1",
            StripeCustomerId = "cus_1",
            StripePriceId = "price_2",
            Status = DainnStripeSubscriptionStatus.Canceled,
            CancelAtPeriodEnd = true
        });

        updated.StripePriceId.Should().Be("price_2");
        updated.Status.Should().Be(DainnStripeSubscriptionStatus.Canceled);
        updated.CancelAtPeriodEnd.Should().BeTrue();
        (await fixture.DbContext.DainnStripeSubscriptions.CountAsync()).Should().Be(1);
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
