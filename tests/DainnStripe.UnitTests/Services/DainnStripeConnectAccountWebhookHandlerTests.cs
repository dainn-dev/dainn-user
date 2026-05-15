using DainnStripe.Data;
using DainnStripe.Entities;
using DainnStripe.Enums;
using DainnStripe.Interfaces;
using DainnStripe.Models;
using DainnStripe.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Stripe;

namespace DainnStripe.UnitTests.Services;

public class DainnStripeConnectAccountWebhookHandlerTests
{
    [Fact]
    public async Task HandleAsync_AccountUpdated_SyncsConnectedAccountCapabilities()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new DainnStripeConnectService(new FakeConnectAccountClient(), fixture.DbContext);
        await service.UpsertTenantAsync(new UpsertTenantRequest
        {
            TenantId = "tenant_1",
            DisplayName = "Tenant One"
        });
        await service.CreateConnectedAccountAsync(new CreateConnectedAccountRequest
        {
            TenantId = "tenant_1",
            OwnerId = "owner_1"
        });
        var handler = new DainnStripeConnectAccountWebhookHandler(service, NullLogger<DainnStripeConnectAccountWebhookHandler>.Instance);
        var record = new StripeWebhookEventRecord
        {
            StripeEventId = "evt_1",
            EventType = "account.updated",
            Payload = """
            {
              "data": {
                "object": {
                  "id": "acct_default",
                  "charges_enabled": true,
                  "payouts_enabled": true,
                  "details_submitted": true,
                  "requirements": {
                    "disabled_reason": null
                  }
                }
              }
            }
            """
        };

        await handler.HandleAsync(new Event { Id = "evt_1", Type = record.EventType }, record);

        var account = await fixture.DbContext.DainnStripeConnectedAccounts.SingleAsync();
        account.Status.Should().Be(DainnStripeConnectedAccountStatus.Active);
        account.ChargesEnabled.Should().BeTrue();
        account.PayoutsEnabled.Should().BeTrue();
        account.DetailsSubmitted.Should().BeTrue();
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
                StripeAccountId = "acct_default",
                Country = "US",
                DefaultCurrency = "usd"
            });
        }

        public Task<ConnectedAccountLinkResult> CreateAccountLinkAsync(
            CreateConnectedAccountLinkRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ConnectedAccountLinkResult
            {
                Url = "https://connect.stripe.test/default"
            });
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
