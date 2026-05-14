using DainnStripe.Data;
using DainnStripe.Enums;
using DainnStripe.Interfaces;
using DainnStripe.Models;
using DainnStripe.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DainnStripe.UnitTests.Services;

public class DainnStripeConnectServiceTests
{
    [Fact]
    public async Task UpsertTenantAsync_ExistingTenant_UpdatesTenant()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new DainnStripeConnectService(new FakeConnectAccountClient(), fixture.DbContext);

        await service.UpsertTenantAsync(new UpsertTenantRequest
        {
            TenantId = "tenant_1",
            DisplayName = "Tenant One",
            DefaultCurrency = "USD"
        });

        var updated = await service.UpsertTenantAsync(new UpsertTenantRequest
        {
            TenantId = "tenant_1",
            DisplayName = "Tenant Updated",
            DefaultCurrency = "EUR",
            Active = false
        });

        updated.DisplayName.Should().Be("Tenant Updated");
        updated.DefaultCurrency.Should().Be("eur");
        updated.Active.Should().BeFalse();
        (await fixture.DbContext.DainnStripeTenants.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateConnectedAccountAsync_NewOwner_CreatesStripeAccountAndPersistsMapping()
    {
        await using var fixture = await CreateFixtureAsync();
        var client = new FakeConnectAccountClient
        {
            AccountResult = new ConnectedAccountResult
            {
                StripeAccountId = "acct_1",
                Email = "owner@example.test",
                Country = "US",
                DefaultCurrency = "usd",
                DetailsSubmitted = false
            }
        };
        var service = new DainnStripeConnectService(client, fixture.DbContext);

        await service.UpsertTenantAsync(new UpsertTenantRequest
        {
            TenantId = "tenant_1",
            DisplayName = "Tenant One"
        });

        var account = await service.CreateConnectedAccountAsync(new CreateConnectedAccountRequest
        {
            TenantId = "tenant_1",
            OwnerId = "owner_1",
            Email = "owner@example.test",
            Country = "US",
            DefaultCurrency = "usd"
        });

        account.StripeAccountId.Should().Be("acct_1");
        account.Status.Should().Be(DainnStripeConnectedAccountStatus.OnboardingRequired);
        client.CreateAccountCalls.Should().Be(1);
    }

    [Fact]
    public async Task CreateConnectedAccountAsync_ExistingOwner_ReturnsExistingWithoutCallingStripe()
    {
        await using var fixture = await CreateFixtureAsync();
        var client = new FakeConnectAccountClient();
        var service = new DainnStripeConnectService(client, fixture.DbContext);

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

        var second = await service.CreateConnectedAccountAsync(new CreateConnectedAccountRequest
        {
            TenantId = "tenant_1",
            OwnerId = "owner_1"
        });

        second.StripeAccountId.Should().Be("acct_default");
        client.CreateAccountCalls.Should().Be(1);
        (await fixture.DbContext.DainnStripeConnectedAccounts.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateOnboardingLinkAsync_ExistingAccount_StoresLink()
    {
        await using var fixture = await CreateFixtureAsync();
        var expiresAt = DateTime.UtcNow.AddMinutes(30);
        var client = new FakeConnectAccountClient
        {
            LinkResult = new ConnectedAccountLinkResult
            {
                Url = "https://connect.stripe.test/onboarding",
                ExpiresAt = expiresAt
            }
        };
        var service = new DainnStripeConnectService(client, fixture.DbContext);

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

        var link = await service.CreateOnboardingLinkAsync(new CreateConnectedAccountLinkRequest
        {
            StripeAccountId = "acct_default",
            ReturnUrl = "https://app.test/return",
            RefreshUrl = "https://app.test/refresh"
        });

        link.Url.Should().Be("https://connect.stripe.test/onboarding");
        var account = await fixture.DbContext.DainnStripeConnectedAccounts.SingleAsync();
        account.OnboardingUrl.Should().Be(link.Url);
        account.OnboardingUrlExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public async Task SyncConnectedAccountAsync_EnabledCapabilities_MarksAccountActive()
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

        var account = await service.SyncConnectedAccountAsync(new SyncConnectedAccountRequest
        {
            StripeAccountId = "acct_default",
            ChargesEnabled = true,
            PayoutsEnabled = true,
            DetailsSubmitted = true
        });

        account.Should().NotBeNull();
        account!.Status.Should().Be(DainnStripeConnectedAccountStatus.Active);
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
        public ConnectedAccountResult AccountResult { get; set; } = new()
        {
            StripeAccountId = "acct_default",
            Country = "US",
            DefaultCurrency = "usd"
        };

        public ConnectedAccountLinkResult LinkResult { get; set; } = new()
        {
            Url = "https://connect.stripe.test/default"
        };

        public int CreateAccountCalls { get; private set; }

        public Task<ConnectedAccountResult> CreateAccountAsync(
            CreateConnectedAccountRequest request,
            CancellationToken cancellationToken = default)
        {
            CreateAccountCalls++;
            return Task.FromResult(AccountResult);
        }

        public Task<ConnectedAccountLinkResult> CreateAccountLinkAsync(
            CreateConnectedAccountLinkRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(LinkResult);
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
