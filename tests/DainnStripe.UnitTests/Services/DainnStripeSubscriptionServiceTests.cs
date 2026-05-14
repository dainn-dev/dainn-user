using DainnStripe.Data;
using DainnStripe.Entities;
using DainnStripe.Enums;
using DainnStripe.Interfaces;
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
        var service = CreateService(fixture.DbContext);
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
        var service = CreateService(fixture.DbContext);

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

    [Fact]
    public async Task CreateAsync_ValidRequest_PersistsSubscription()
    {
        await using var fixture = await CreateFixtureAsync();
        var client = new FakeSubscriptionClient(new SubscriptionResult
        {
            StripeSubscriptionId = "sub_new",
            StripeCustomerId = "cus_1",
            Status = DainnStripeSubscriptionStatus.Active
        });
        var service = CreateService(fixture.DbContext, client);

        var subscription = await service.CreateAsync(new CreateSubscriptionRequest
        {
            OwnerId = "user_1",
            StripeCustomerId = "cus_1",
            StripePriceId = "price_1"
        });

        subscription.OwnerId.Should().Be("user_1");
        subscription.StripeSubscriptionId.Should().Be("sub_new");
        subscription.Status.Should().Be(DainnStripeSubscriptionStatus.Active);
        (await fixture.DbContext.DainnStripeSubscriptions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_MissingOwnerId_Throws()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = CreateService(fixture.DbContext);

        var act = async () => await service.CreateAsync(new CreateSubscriptionRequest
        {
            OwnerId = "",
            StripeCustomerId = "cus_1",
            StripePriceId = "price_1"
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CancelAsync_ExistingSubscription_MarksCanceled()
    {
        await using var fixture = await CreateFixtureAsync();

        fixture.DbContext.DainnStripeSubscriptions.Add(new DainnStripeSubscription
        {
            Id = Guid.NewGuid(),
            OwnerId = "user_1",
            StripeSubscriptionId = "sub_cancel",
            StripeCustomerId = "cus_1",
            Status = DainnStripeSubscriptionStatus.Active
        });
        await fixture.DbContext.SaveChangesAsync();

        var client = new FakeSubscriptionClient(new SubscriptionResult
        {
            StripeSubscriptionId = "sub_cancel",
            StripeCustomerId = "cus_1",
            Status = DainnStripeSubscriptionStatus.Canceled
        });
        var service = CreateService(fixture.DbContext, client);

        var subscription = await service.CancelAsync("user_1", "sub_cancel");

        subscription.Status.Should().Be(DainnStripeSubscriptionStatus.Canceled);
        subscription.CanceledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelAsync_NotFound_Throws()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = CreateService(fixture.DbContext);

        var act = async () => await service.CancelAsync("user_1", "sub_nonexistent");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static DainnStripeSubscriptionService CreateService(
        DainnStripeDbContext dbContext,
        IDainnStripeSubscriptionClient? client = null)
    {
        return new DainnStripeSubscriptionService(client ?? new FakeSubscriptionClient(null), dbContext);
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

    private sealed class FakeSubscriptionClient : IDainnStripeSubscriptionClient
    {
        private readonly SubscriptionResult? _result;

        public FakeSubscriptionClient(SubscriptionResult? result)
        {
            _result = result;
        }

        public Task<SubscriptionResult> CreateAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result ?? throw new InvalidOperationException("No fake result configured."));
        }

        public Task<SubscriptionResult> CancelAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result ?? throw new InvalidOperationException("No fake result configured."));
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
