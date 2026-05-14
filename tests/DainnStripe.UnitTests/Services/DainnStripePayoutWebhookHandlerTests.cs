using DainnStripe.Data;
using DainnStripe.Entities;
using DainnStripe.Interfaces;
using DainnStripe.Models;
using DainnStripe.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace DainnStripe.UnitTests.Services;

public class DainnStripePayoutWebhookHandlerTests
{
    [Fact]
    public async Task HandleAsync_PayoutPaid_UpsertsPayout()
    {
        await using var fixture = await CreateFixtureAsync();
        await SeedConnectedAccountAsync(fixture.DbContext);
        var handler = new DainnStripePayoutWebhookHandler(fixture.DbContext);
        var record = new StripeWebhookEventRecord
        {
            EventType = "payout.paid",
            StripeAccountId = "acct_1",
            Payload = """
                      {
                        "id": "evt_1",
                        "type": "payout.paid",
                        "account": "acct_1",
                        "data": {
                          "object": {
                            "id": "po_1",
                            "amount": 4200,
                            "currency": "usd",
                            "destination": "ba_1",
                            "method": "standard",
                            "status": "paid",
                            "arrival_date": 1715904000
                          }
                        }
                      }
                      """
        };

        await handler.HandleAsync(new Event { Type = "payout.paid" }, record);

        var payout = await fixture.DbContext.DainnStripePayouts.SingleAsync();
        payout.StripePayoutId.Should().Be("po_1");
        payout.StripeAccountId.Should().Be("acct_1");
        payout.OwnerId.Should().Be("owner_1");
        payout.Amount.Should().Be(4200);
        payout.Currency.Should().Be("usd");
        payout.Status.Should().Be("paid");
        payout.Destination.Should().Be("ba_1");
        payout.ArrivalDate.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1715904000).UtcDateTime);
    }

    [Fact]
    public async Task HandleAsync_BalanceAvailable_PersistsSnapshot()
    {
        await using var fixture = await CreateFixtureAsync();
        await SeedConnectedAccountAsync(fixture.DbContext);
        var handler = new DainnStripeBalanceWebhookHandler(fixture.DbContext);
        var record = new StripeWebhookEventRecord
        {
            EventType = "balance.available",
            StripeAccountId = "acct_1",
            Payload = """
                      {
                        "id": "evt_2",
                        "type": "balance.available",
                        "account": "acct_1",
                        "livemode": false,
                        "data": {
                          "object": {
                            "available": [{ "amount": 1200, "currency": "usd" }],
                            "pending": [{ "amount": 300, "currency": "usd" }]
                          }
                        }
                      }
                      """
        };

        await handler.HandleAsync(new Event { Type = "balance.available" }, record);

        var snapshot = await fixture.DbContext.DainnStripeBalanceSnapshots.SingleAsync();
        snapshot.StripeAccountId.Should().Be("acct_1");
        snapshot.AvailableJson.Should().Contain("\"amount\": 1200");
        snapshot.PendingJson.Should().Contain("\"amount\": 300");
        snapshot.ConnectedAccountId.Should().NotBeNull();
    }

    private static async Task SeedConnectedAccountAsync(DainnStripeDbContext dbContext)
    {
        var connectService = new DainnStripeConnectService(new FakeConnectAccountClient(), dbContext);
        await connectService.UpsertTenantAsync(new UpsertTenantRequest
        {
            TenantId = "tenant_1",
            DisplayName = "Tenant One"
        });
        await connectService.CreateConnectedAccountAsync(new CreateConnectedAccountRequest
        {
            TenantId = "tenant_1",
            OwnerId = "owner_1"
        });
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

    private sealed class FakeConnectAccountClient : IDainnStripeConnectAccountClient
    {
        public Task<ConnectedAccountResult> CreateAccountAsync(
            CreateConnectedAccountRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ConnectedAccountResult
            {
                StripeAccountId = "acct_1",
                Country = "US",
                DefaultCurrency = "usd",
                ChargesEnabled = true,
                PayoutsEnabled = true,
                DetailsSubmitted = true
            });
        }

        public Task<ConnectedAccountLinkResult> CreateAccountLinkAsync(
            CreateConnectedAccountLinkRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ConnectedAccountLinkResult());
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
