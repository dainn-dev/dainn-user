using DainnStripe.Data;
using DainnStripe.Entities;
using DainnStripe.Interfaces;
using DainnStripe.Models;
using DainnStripe.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DainnStripe.UnitTests.Services;

public class DainnStripeReconciliationServiceTests
{
    [Fact]
    public async Task ReconcileMoneyMovementAsync_RefreshesTransfersPayoutsAndCapturesBalance()
    {
        await using var fixture = await CreateFixtureAsync();
        var account = await SeedConnectedAccountAsync(fixture.DbContext);
        fixture.DbContext.DainnStripeTransfers.Add(new DainnStripeTransfer
        {
            Id = Guid.NewGuid(),
            ConnectedAccountId = account.Id,
            OwnerId = "owner_1",
            StripeTransferId = "tr_1",
            StripeDestinationAccountId = "acct_1",
            Amount = 1000,
            Currency = "usd"
        });
        fixture.DbContext.DainnStripePayouts.Add(new DainnStripePayout
        {
            Id = Guid.NewGuid(),
            ConnectedAccountId = account.Id,
            OwnerId = "owner_1",
            StripePayoutId = "po_1",
            StripeAccountId = "acct_1",
            Amount = 500,
            Currency = "usd",
            Status = "pending"
        });
        await fixture.DbContext.SaveChangesAsync();

        var service = new DainnStripeReconciliationService(
            new FakeTransferClient(),
            new FakePayoutClient(),
            new FakeBalanceClient(),
            fixture.DbContext);

        var result = await service.ReconcileMoneyMovementAsync(new ReconcileMoneyMovementRequest
        {
            OwnerId = "owner_1"
        });

        result.TransfersReconciled.Should().Be(1);
        result.PayoutsReconciled.Should().Be(1);
        result.BalanceSnapshotsCaptured.Should().Be(1);

        var transfer = await fixture.DbContext.DainnStripeTransfers.SingleAsync();
        transfer.Amount.Should().Be(1500);
        transfer.Reversed.Should().BeTrue();

        var payout = await fixture.DbContext.DainnStripePayouts.SingleAsync();
        payout.Amount.Should().Be(700);
        payout.Status.Should().Be("paid");

        var snapshot = await fixture.DbContext.DainnStripeBalanceSnapshots.SingleAsync();
        snapshot.StripeAccountId.Should().Be("acct_1");
        snapshot.AvailableJson.Should().Contain("available");
    }

    private static async Task<DainnStripeConnectedAccount> SeedConnectedAccountAsync(DainnStripeDbContext dbContext)
    {
        var tenant = new DainnStripeTenant
        {
            Id = Guid.NewGuid(),
            TenantId = "tenant_1",
            DisplayName = "Tenant One",
            DefaultCurrency = "usd"
        };
        var account = new DainnStripeConnectedAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            OwnerId = "owner_1",
            StripeAccountId = "acct_1",
            Country = "US",
            DefaultCurrency = "usd",
            ChargesEnabled = true,
            PayoutsEnabled = true,
            DetailsSubmitted = true
        };

        dbContext.DainnStripeTenants.Add(tenant);
        dbContext.DainnStripeConnectedAccounts.Add(account);
        await dbContext.SaveChangesAsync();
        return account;
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

    private sealed class FakeTransferClient : IDainnStripeTransferClient
    {
        public Task<TransferResult> CreateAsync(
            CreateTransferRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new TransferResult());
        }

        public Task<TransferResult> GetAsync(
            string transferId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new TransferResult
            {
                TransferId = transferId,
                DestinationAccountId = "acct_1",
                Amount = 1500,
                Currency = "USD",
                Reversed = true
            });
        }
    }

    private sealed class FakePayoutClient : IDainnStripePayoutClient
    {
        public Task<PayoutResult> CreateAsync(
            CreatePayoutRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PayoutResult());
        }

        public Task<PayoutResult> GetAsync(
            string payoutId,
            string stripeAccountId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PayoutResult
            {
                PayoutId = payoutId,
                StripeAccountId = stripeAccountId,
                Amount = 700,
                Currency = "USD",
                Status = "paid"
            });
        }
    }

    private sealed class FakeBalanceClient : IDainnStripeBalanceClient
    {
        public Task<BalanceSnapshotResult> GetAsync(
            string? stripeAccountId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new BalanceSnapshotResult
            {
                StripeAccountId = stripeAccountId,
                AvailableJson = """[{ "kind": "available" }]""",
                PendingJson = "[]"
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
