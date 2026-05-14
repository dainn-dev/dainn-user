using DainnStripe.Data;
using DainnStripe.Entities;
using DainnStripe.Interfaces;
using DainnStripe.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace DainnStripe.Services;

/// <summary>
/// Syncs the local managed catalog from active Stripe products and prices.
/// </summary>
public class DainnStripeCatalogSyncService : IDainnStripeCatalogSyncService
{
    private readonly IDainnStripeClientFactory _clientFactory;
    private readonly DainnStripeDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeCatalogSyncService"/> class.
    /// </summary>
    public DainnStripeCatalogSyncService(
        IDainnStripeClientFactory clientFactory,
        DainnStripeDbContext dbContext)
    {
        _clientFactory = clientFactory;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<CatalogSyncResult> SyncFromStripeAsync(
        string? triggerSource = null,
        CancellationToken cancellationToken = default)
    {
        var run = new DainnStripeCatalogSyncRun
        {
            Id = Guid.NewGuid(),
            TriggerSource = triggerSource ?? "manual",
            StartedAt = DateTime.UtcNow
        };
        _dbContext.DainnStripeCatalogSyncRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var client = _clientFactory.CreateClient();
            var productService = new ProductService(client);
            var priceService = new PriceService(client);
            var now = DateTime.UtcNow;

            var productList = await productService.ListAsync(
                new ProductListOptions { Active = true, Limit = 100 },
                null,
                cancellationToken);

            foreach (var sp in productList)
            {
                var existing = await _dbContext.DainnStripeProducts
                    .SingleOrDefaultAsync(p => p.StripeProductId == sp.Id, cancellationToken);

                if (existing is null)
                {
                    _dbContext.DainnStripeProducts.Add(new DainnStripeProduct
                    {
                        Id = Guid.NewGuid(),
                        LookupKey = sp.Id,
                        StripeProductId = sp.Id,
                        Name = sp.Name,
                        Description = sp.Description,
                        Active = sp.Active,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                    run.ProductsCreated++;
                }
                else
                {
                    existing.Name = sp.Name;
                    existing.Description = sp.Description;
                    existing.Active = sp.Active;
                    existing.UpdatedAt = now;
                    run.ProductsUpdated++;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            foreach (var sp in productList)
            {
                var product = await _dbContext.DainnStripeProducts
                    .SingleOrDefaultAsync(p => p.StripeProductId == sp.Id, cancellationToken);

                if (product is null)
                {
                    continue;
                }

                var priceList = await priceService.ListAsync(
                    new PriceListOptions { Product = sp.Id, Limit = 100 },
                    null,
                    cancellationToken);

                foreach (var sp2 in priceList)
                {
                    var existingPrice = await _dbContext.DainnStripePrices
                        .SingleOrDefaultAsync(p => p.StripePriceId == sp2.Id, cancellationToken);

                    if (existingPrice is null)
                    {
                        _dbContext.DainnStripePrices.Add(new DainnStripePrice
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            LookupKey = sp2.Id,
                            StripePriceId = sp2.Id,
                            Currency = sp2.Currency,
                            UnitAmount = sp2.UnitAmount ?? 0,
                            Active = sp2.Active,
                            CreatedAt = now,
                            UpdatedAt = now
                        });
                        run.PricesCreated++;
                    }
                    else
                    {
                        existingPrice.UnitAmount = sp2.UnitAmount ?? 0;
                        existingPrice.Active = sp2.Active;
                        existingPrice.UpdatedAt = now;
                        run.PricesUpdated++;
                    }
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            run.Succeeded = true;
            run.CompletedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new CatalogSyncResult
            {
                SyncRunId = run.Id,
                Succeeded = true,
                ProductsCreated = run.ProductsCreated,
                ProductsUpdated = run.ProductsUpdated,
                PricesCreated = run.PricesCreated,
                PricesUpdated = run.PricesUpdated
            };
        }
        catch (Exception ex)
        {
            run.Succeeded = false;
            run.ErrorMessage = ex.Message.Length > 2048 ? ex.Message[..2048] : ex.Message;
            run.CompletedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }
}
