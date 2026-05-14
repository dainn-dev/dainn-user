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
    [Fact]
    public async Task UpsertProductAsync_ExistingLookupKey_UpdatesProduct()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new DainnStripeCatalogService(fixture.DbContext);

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
            Description = "Updated plan",
            Active = false
        });

        updated.Name.Should().Be("Pro Updated");
        updated.Description.Should().Be("Updated plan");
        updated.Active.Should().BeFalse();
        (await fixture.DbContext.DainnStripeProducts.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task UpsertPriceAsync_WithExistingProduct_CreatesPriceAndActiveCatalogReturnsIt()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new DainnStripeCatalogService(fixture.DbContext);

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
        catalog[0].Prices.Should().ContainSingle(item => item.LookupKey == "pro_monthly");
    }

    [Fact]
    public async Task UpsertPriceAsync_MissingProduct_Throws()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new DainnStripeCatalogService(fixture.DbContext);

        var request = new UpsertCatalogPriceRequest
        {
            ProductLookupKey = "missing",
            LookupKey = "missing_monthly",
            StripePriceId = "price_missing",
            UnitAmount = 1000,
            Interval = DainnStripePriceInterval.Month
        };

        await service.Invoking(item => item.UpsertPriceAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DainnStripe product 'missing' does not exist.");
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
