using DainnStripe.Data;
using DainnStripe.Entities;
using DainnStripe.Interfaces;
using DainnStripe.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace DainnStripe.Services;

/// <summary>
/// Default managed catalog service.
/// When <see cref="IDainnStripeClientFactory"/> is supplied, products and prices without a Stripe ID
/// are automatically provisioned on Stripe and the IDs are persisted locally.
/// </summary>
public class DainnStripeCatalogService : IDainnStripeCatalogService
{
    private readonly IDainnStripeClientFactory? _clientFactory;
    private readonly DainnStripeDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeCatalogService"/> class.
    /// </summary>
    /// <param name="clientFactory">
    /// Optional Stripe client factory. When provided, products and prices without a Stripe ID
    /// are created automatically on Stripe.
    /// </param>
    /// <param name="dbContext">EF Core database context.</param>
    public DainnStripeCatalogService(
        IDainnStripeClientFactory? clientFactory,
        DainnStripeDbContext dbContext)
    {
        _clientFactory = clientFactory;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<DainnStripeProduct> UpsertProductAsync(
        UpsertCatalogProductRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureRequired(request.LookupKey, nameof(request.LookupKey));
        EnsureRequired(request.Name, nameof(request.Name));

        var product = await _dbContext.DainnStripeProducts
            .SingleOrDefaultAsync(item => item.LookupKey == request.LookupKey, cancellationToken);

        var now = DateTime.UtcNow;
        if (product is null)
        {
            product = new DainnStripeProduct
            {
                Id = Guid.NewGuid(),
                LookupKey = request.LookupKey,
                CreatedAt = now
            };
            _dbContext.DainnStripeProducts.Add(product);
        }

        // Auto-provision on Stripe when no StripeProductId is provided
        if (string.IsNullOrWhiteSpace(request.StripeProductId) && _clientFactory is not null)
        {
            var stripeProductId = string.IsNullOrWhiteSpace(product.StripeProductId)
                ? await CreateStripeProductAsync(request, cancellationToken)
                : product.StripeProductId;

            product.StripeProductId = stripeProductId;
        }
        else if (!string.IsNullOrWhiteSpace(request.StripeProductId))
        {
            product.StripeProductId = request.StripeProductId;
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.Active = request.Active;
        product.MetadataJson = request.MetadataJson;
        product.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return product;
    }

    /// <inheritdoc />
    public async Task<DainnStripePrice> UpsertPriceAsync(
        UpsertCatalogPriceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureRequired(request.ProductLookupKey, nameof(request.ProductLookupKey));
        EnsureRequired(request.LookupKey, nameof(request.LookupKey));
        EnsureRequired(request.Currency, nameof(request.Currency));

        if (request.UnitAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.UnitAmount), "Unit amount must not be negative.");
        }

        var product = await _dbContext.DainnStripeProducts
            .SingleOrDefaultAsync(item => item.LookupKey == request.ProductLookupKey, cancellationToken)
            ?? throw new InvalidOperationException(
                $"DainnStripe product '{request.ProductLookupKey}' does not exist.");

        var price = await _dbContext.DainnStripePrices
            .SingleOrDefaultAsync(item => item.LookupKey == request.LookupKey, cancellationToken);

        var now = DateTime.UtcNow;
        if (price is null)
        {
            price = new DainnStripePrice
            {
                Id = Guid.NewGuid(),
                LookupKey = request.LookupKey,
                CreatedAt = now
            };
            _dbContext.DainnStripePrices.Add(price);
        }

        // Auto-provision on Stripe when no StripePriceId is provided
        if (string.IsNullOrWhiteSpace(request.StripePriceId) && _clientFactory is not null)
        {
            if (string.IsNullOrWhiteSpace(product.StripeProductId))
            {
                throw new InvalidOperationException(
                    $"Cannot auto-create a Stripe Price for product '{product.LookupKey}' because it has no StripeProductId. " +
                    "Call UpsertProductAsync first so the product is provisioned on Stripe.");
            }

            var stripePriceId = string.IsNullOrWhiteSpace(price.StripePriceId)
                ? await CreateStripePriceAsync(request, product.StripeProductId, cancellationToken)
                : price.StripePriceId;

            price.StripePriceId = stripePriceId;
        }
        else if (!string.IsNullOrWhiteSpace(request.StripePriceId))
        {
            price.StripePriceId = request.StripePriceId;
        }

        price.ProductId = product.Id;
        price.Currency = request.Currency.ToLowerInvariant();
        price.UnitAmount = request.UnitAmount;
        price.Interval = request.Interval;
        price.IntervalCount = request.IntervalCount;
        price.Active = request.Active;
        price.MetadataJson = request.MetadataJson;
        price.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return price;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DainnStripeProduct>> GetActiveCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.DainnStripeProducts
            .AsNoTracking()
            .Where(product => product.Active)
            .Include(product => product.Prices.Where(price => price.Active))
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CatalogPage> GetActiveCatalogPagedAsync(
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var totalCount = await _dbContext.DainnStripeProducts
            .CountAsync(product => product.Active, cancellationToken);

        var items = await _dbContext.DainnStripeProducts
            .AsNoTracking()
            .Where(product => product.Active)
            .Include(product => product.Prices.Where(price => price.Active))
            .OrderBy(product => product.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new CatalogPage
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private async Task<string> CreateStripeProductAsync(
        UpsertCatalogProductRequest request,
        CancellationToken cancellationToken)
    {
        var client = _clientFactory!.CreateClient();
        var productService = new ProductService(client);
        var stripeProduct = await productService.CreateAsync(
            new ProductCreateOptions
            {
                Name = request.Name,
                Description = request.Description,
                Active = request.Active
            },
            cancellationToken: cancellationToken);

        return stripeProduct.Id;
    }

    private async Task<string> CreateStripePriceAsync(
        UpsertCatalogPriceRequest request,
        string stripeProductId,
        CancellationToken cancellationToken)
    {
        var client = _clientFactory!.CreateClient();
        var priceService = new PriceService(client);

        var options = new PriceCreateOptions
        {
            Product = stripeProductId,
            UnitAmount = request.UnitAmount,
            Currency = request.Currency.ToLowerInvariant(),
            Active = request.Active
        };

        if (request.Interval != Enums.DainnStripePriceInterval.None)
        {
            options.Recurring = new PriceRecurringOptions
            {
                Interval = MapInterval(request.Interval),
                IntervalCount = request.IntervalCount ?? 1
            };
        }

        var stripePrice = await priceService.CreateAsync(options, cancellationToken: cancellationToken);
        return stripePrice.Id;
    }

    private static string MapInterval(Enums.DainnStripePriceInterval interval) => interval switch
    {
        Enums.DainnStripePriceInterval.Day => "day",
        Enums.DainnStripePriceInterval.Week => "week",
        Enums.DainnStripePriceInterval.Month => "month",
        Enums.DainnStripePriceInterval.Year => "year",
        _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null)
    };

    private static void EnsureRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
