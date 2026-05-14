using DainnStripe.Data;
using DainnStripe.Entities;
using DainnStripe.Interfaces;
using DainnStripe.Models;
using Microsoft.EntityFrameworkCore;

namespace DainnStripe.Services;

/// <summary>
/// Default managed catalog service.
/// </summary>
public class DainnStripeCatalogService : IDainnStripeCatalogService
{
    private readonly DainnStripeDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeCatalogService"/> class.
    /// </summary>
    public DainnStripeCatalogService(DainnStripeDbContext dbContext)
    {
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

        product.StripeProductId = request.StripeProductId;
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

        price.ProductId = product.Id;
        price.StripePriceId = request.StripePriceId;
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
    public async Task<IReadOnlyList<DainnStripeProduct>> GetActiveCatalogAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.DainnStripeProducts
            .AsNoTracking()
            .Where(product => product.Active)
            .Include(product => product.Prices.Where(price => price.Active))
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);
    }

    private static void EnsureRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
