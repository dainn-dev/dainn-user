using DainnStripe.Data;
using DainnStripe.Enums;
using DainnStripe.Models;
using DainnStripe.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DainnStripe.UnitTests.Services;

public class DainnStripeCatalogServiceTests
{
    // ── UpsertProductAsync ────────────────────────────────────────────────

    [Fact]
    public async Task UpsertProductAsync_NewProduct_CreatesLocalRecord()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new DainnStripeCatalogService(null, fixture.DbContext);

        var product = await service.UpsertProductAsync(new UpsertCatalogProductRequest
        {
            LookupKey = "pro",
            StripeProductId = "prod_1",
            Name = "Pro"
        });

        product.LookupKey.Should().Be("pro");
        product.StripeProductId.Should().Be("prod_1");
        (await fixture.DbContext.DainnStripeProducts.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task UpsertProductAsync_ExistingLookupKey_UpdatesProduct()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new DainnStripeCatalogService(null, fixture.DbContext);

        await service.UpsertProductAsync(new UpsertCatalogProductRequest
        {
            LookupKey = "pro",
            StripeProductId = "prod_1",
            Name = "Pro"
        });

        var updated = await service.UpsertProductAsync(new UpsertCatalogProductRequest
        {
            LookupKey = "pro",
            StripeProductId = "prod_1",
            Name = "Pro Updated",
            Description = "Updated description",
            Active = false
        });

        updated.Name.Should().Be("Pro Updated");
        updated.Description.Should().Be("Updated description");
        updated.Active.Should().BeFalse();
        (await fixture.DbContext.DainnStripeProducts.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task UpsertProductAsync_NoStripeIdAndNoFactory_LeavesStripeProductIdNull()
    {
        await using var fixture = await CreateFixtureAsync();
        // No client factory supplied — auto-provisioning on Stripe is disabled
        var service = new DainnStripeCatalogService(null, fixture.DbContext);

        var product = await service.UpsertProductAsync(new UpsertCatalogProductRequest
        {
            LookupKey = "local-only",
            Name = "Local Only Product"
            // StripeProductId intentionally omitted
        });

        product.StripeProductId.Should().BeNull();
    }

    // ── UpsertPriceAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task UpsertPriceAsync_WithExistingProduct_CreatesPriceAndActiveCatalogReturnsIt()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new DainnStripeCatalogService(null, fixture.DbContext);

        await service.UpsertProductAsync(new UpsertCatalogProductRequest
        {
            LookupKey = "pro",
            StripeProductId = "prod_1",
            Name = "Pro"
        });

        var price = await service.UpsertPriceAsync(new UpsertCatalogPriceRequest
        {
            ProductLookupKey = "pro",
            LookupKey = "pro_monthly",
            StripePriceId = "price_1",
            Currency = "USD",
            UnitAmount = 2900,
            Interval = DainnStripePriceInterval.Month,
            IntervalCount = 1
        });

        price.Currency.Should().Be("usd");
        price.UnitAmount.Should().Be(2900);

        var catalog = await service.GetActiveCatalogAsync();
        catalog.Should().ContainSingle();
        catalog[0].Prices.Should().ContainSingle(p => p.LookupKey == "pro_monthly");
    }

    [Fact]
    public async Task UpsertPriceAsync_MissingProduct_Throws()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new DainnStripeCatalogService(null, fixture.DbContext);

        var request = new UpsertCatalogPriceRequest
        {
            ProductLookupKey = "missing",
            LookupKey = "missing_monthly",
            StripePriceId = "price_missing",
            UnitAmount = 1000,
            Interval = DainnStripePriceInterval.Month
        };

        await service.Invoking(s => s.UpsertPriceAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DainnStripe product 'missing' does not exist.");
    }

    [Fact]
    public async Task UpsertPriceAsync_NegativeAmount_Throws()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new DainnStripeCatalogService(null, fixture.DbContext);

        await service.UpsertProductAsync(new UpsertCatalogProductRequest
        {
            LookupKey = "pro",
            StripeProductId = "prod_1",
            Name = "Pro"
        });

        var request = new UpsertCatalogPriceRequest
        {
            ProductLookupKey = "pro",
            LookupKey = "bad_price",
            StripePriceId = "price_bad",
            UnitAmount = -1,
            Currency = "usd"
        };

        await service.Invoking(s => s.UpsertPriceAsync(request))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    // ── GetActiveCatalogPagedAsync ────────────────────────────────────────

    [Fact]
    public async Task GetActiveCatalogPagedAsync_FirstPage_ReturnsCorrectSlice()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new DainnStripeCatalogService(null, fixture.DbContext);

        for (var i = 1; i <= 5; i++)
        {
            await service.UpsertProductAsync(new UpsertCatalogProductRequest
            {
                LookupKey = $"product-{i:D2}",
                StripeProductId = $"prod_{i}",
                Name = $"Product {i:D2}"
            });
        }

        var page = await service.GetActiveCatalogPagedAsync(page: 1, pageSize: 3);

        page.Items.Should().HaveCount(3);
        page.TotalCount.Should().Be(5);
        page.TotalPages.Should().Be(2);
        page.HasNextPage.Should().BeTrue();
        page.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetActiveCatalogPagedAsync_LastPage_ReturnsRemainder()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new DainnStripeCatalogService(null, fixture.DbContext);

        for (var i = 1; i <= 5; i++)
        {
            await service.UpsertProductAsync(new UpsertCatalogProductRequest
            {
                LookupKey = $"product-{i:D2}",
                StripeProductId = $"prod_{i}",
                Name = $"Product {i:D2}"
            });
        }

        var page = await service.GetActiveCatalogPagedAsync(page: 2, pageSize: 3);

        page.Items.Should().HaveCount(2);
        page.HasNextPage.Should().BeFalse();
        page.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveCatalogPagedAsync_ExcludesInactiveProducts()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new DainnStripeCatalogService(null, fixture.DbContext);

        await service.UpsertProductAsync(new UpsertCatalogProductRequest
        {
            LookupKey = "active", StripeProductId = "prod_a", Name = "Active", Active = true
        });
        await service.UpsertProductAsync(new UpsertCatalogProductRequest
        {
            LookupKey = "inactive", StripeProductId = "prod_b", Name = "Inactive", Active = false
        });

        var page = await service.GetActiveCatalogPagedAsync(page: 1, pageSize: 50);

        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle(p => p.LookupKey == "active");
    }

    [Fact]
    public async Task GetActiveCatalogPagedAsync_PageSizeClampsTo200()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new DainnStripeCatalogService(null, fixture.DbContext);

        // Requesting pageSize=999 should be silently clamped to 200
        var page = await service.GetActiveCatalogPagedAsync(page: 1, pageSize: 999);
        page.PageSize.Should().Be(200);
    }

    [Fact]
    public async Task GetActiveCatalogPagedAsync_NegativePageClampsToOne()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new DainnStripeCatalogService(null, fixture.DbContext);

        var page = await service.GetActiveCatalogPagedAsync(page: -5, pageSize: 10);
        page.Page.Should().Be(1);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

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
