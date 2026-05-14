using DainnStripe.Data;
using DainnStripe.Enums;
using DainnStripe.Interfaces;
using DainnStripe.Models;
using DainnStripe.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DainnStripe.UnitTests.Services;

public class DainnStripeMoneyMovementServiceTests
{
    [Fact]
    public async Task CreateTransferAsync_ConnectedAccount_PersistsTransfer()
    {
        await using var fixture = await CreateFixtureAsync();
        await SeedConnectedAccountAsync(fixture.DbContext);
        var transferClient = new FakeTransferClient(new TransferResult
        {
            TransferId = "tr_1",
            DestinationAccountId = "acct_1",
            SourceTransactionId = "ch_1",
            Amount = 2500,
            Currency = "USD",
            TransferGroup = "order_1"
        });
        var service = new DainnStripeMoneyMovementService(
            transferClient,
            new FakePayoutClient(new PayoutResult()),
            fixture.DbContext);

        var result = await service.CreateTransferAsync(new CreateTransferRequest
        {
            OwnerId = "owner_1",
            StripeDestinationAccountId = "acct_1",
            Amount = 2500,
            Currency = "usd",
            StripeSourceTransactionId = "ch_1",
            TransferGroup = "order_1"
        });

        result.TransferId.Should().Be("tr_1");

        var transfer = await fixture.DbContext.DainnStripeTransfers.SingleAsync();
        transfer.OwnerId.Should().Be("owner_1");
        transfer.StripeTransferId.Should().Be("tr_1");
        transfer.StripeDestinationAccountId.Should().Be("acct_1");
        transfer.StripeSourceTransactionId.Should().Be("ch_1");
        transfer.Amount.Should().Be(2500);
        transfer.Currency.Should().Be("usd");
        transfer.TransferGroup.Should().Be("order_1");
    }

    [Fact]
    public async Task CreatePayoutAsync_ConnectedAccount_PersistsPayout()
    {
        await using var fixture = await CreateFixtureAsync();
        await SeedConnectedAccountAsync(fixture.DbContext);
        var arrivalDate = DateTime.UtcNow.Date.AddDays(2);
        var payoutClient = new FakePayoutClient(new PayoutResult
        {
            PayoutId = "po_1",
            StripeAccountId = "acct_1",
            Amount = 3000,
            Currency = "USD",
            Destination = "ba_1",
            Method = "standard",
            Status = "pending",
            ArrivalDate = arrivalDate
        });
        var service = new DainnStripeMoneyMovementService(
            new FakeTransferClient(new TransferResult()),
            payoutClient,
            fixture.DbContext);

        var result = await service.CreatePayoutAsync(new CreatePayoutRequest
        {
            OwnerId = "owner_1",
            StripeAccountId = "acct_1",
            Amount = 3000,
            Currency = "usd",
            Destination = "ba_1"
        });

        result.PayoutId.Should().Be("po_1");

        var payout = await fixture.DbContext.DainnStripePayouts.SingleAsync();
        payout.OwnerId.Should().Be("owner_1");
        payout.StripePayoutId.Should().Be("po_1");
        payout.StripeAccountId.Should().Be("acct_1");
        payout.Amount.Should().Be(3000);
        payout.Currency.Should().Be("usd");
        payout.Destination.Should().Be("ba_1");
        payout.Status.Should().Be("pending");
        payout.ArrivalDate.Should().Be(arrivalDate);
    }

    [Fact]
    public async Task CreateTransferAsync_UnknownConnectedAccount_ThrowsBeforeStripeCall()
    {
        await using var fixture = await CreateFixtureAsync();
        var transferClient = new FakeTransferClient(new TransferResult());
        var service = new DainnStripeMoneyMovementService(
            transferClient,
            new FakePayoutClient(new PayoutResult()),
            fixture.DbContext);

        var request = new CreateTransferRequest
        {
            OwnerId = "owner_1",
            StripeDestinationAccountId = "acct_missing",
            Amount = 1000,
            Currency = "usd"
        };

        await service.Invoking(item => item.CreateTransferAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DainnStripe connected account 'acct_missing' does not exist*");

        transferClient.CreateCalls.Should().Be(0);
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

    private sealed class FakeTransferClient : IDainnStripeTransferClient
    {
        private readonly TransferResult _result;

        public FakeTransferClient(TransferResult result)
        {
            _result = result;
        }

        public int CreateCalls { get; private set; }

        public Task<TransferResult> CreateAsync(
            CreateTransferRequest request,
            CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            return Task.FromResult(_result);
        }

        public Task<TransferResult> GetAsync(
            string transferId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }
    }

    private sealed class FakePayoutClient : IDainnStripePayoutClient
    {
        private readonly PayoutResult _result;

        public FakePayoutClient(PayoutResult result)
        {
            _result = result;
        }

        public Task<PayoutResult> CreateAsync(
            CreatePayoutRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }

        public Task<PayoutResult> GetAsync(
            string payoutId,
            string stripeAccountId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }
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
