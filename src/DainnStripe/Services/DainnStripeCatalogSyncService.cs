using DainnStripe.Data;
using DainnStripe.Entities;
using DainnStripe.Interfaces;
using DainnStripe.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace DainnStripe.Services;

/// <summary>
/// Syncs the local managed catalog from active Stripe products and prices.
/// Uses Stripe auto-pagination to handle catalogs of any size and batch-loads
/// existing local records to avoid N+1 queries.
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

            // Collect all active Stripe products via auto-pagination (handles any catalog size)
            var stripeProducts = new List<Product>();
            await foreach (var sp in productService.ListAutoPagingAsync(
                new ProductListOptions { Active = true, Limit = 100 },
                null,
                cancellationToken))
            {
                stripeProducts.Add(sp);
            }

            // Batch-load all matching local products in a single query to avoid N+1
            var stripeProductIds = stripeProducts.Select(p => p.Id).ToList();
            var existingProductMap = await _dbContext.DainnStripeProducts
                .Where(p => p.StripeProductId != null && stripeProductIds.Contains(p.StripeProductId))
                .ToDictionaryAsync(p => p.StripeProductId!, cancellationToken);

            // Upsert products and build a lookup map for price sync
            var localProductMap = new Dictionary<string, DainnStripeProduct>(stripeProducts.Count);
            foreach (var sp in stripeProducts)
            {
                if (existingProductMap.TryGetValue(sp.Id, out var existing))
                {
                    existing.Name = sp.Name;
                    existing.Description = sp.Description;
                    existing.Active = sp.Active;
                    existing.UpdatedAt = now;
                    run.ProductsUpdated++;
                    localProductMap[sp.Id] = existing;
                }
                else
                {
                    var newProduct = new DainnStripeProduct
                    {
                        Id = Guid.NewGuid(),
                        LookupKey = sp.Id,
                        StripeProductId = sp.Id,
                        Name = sp.Name,
                        Description = sp.Description,
                        Active = sp.Active,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    _dbContext.DainnStripeProducts.Add(newProduct);
                    run.ProductsCreated++;
                    localProductMap[sp.Id] = newProduct;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Sync prices per product; each product uses auto-pagination and a single batch query
            foreach (var sp in stripeProducts)
            {
                if (!localProductMap.TryGetValue(sp.Id, out var localProduct))
                {
                    continue;
                }

                // Collect all Stripe prices for this product via auto-pagination
                var stripePrices = new List<Price>();
                await foreach (var sp2 in priceService.ListAutoPagingAsync(
                    new PriceListOptions { Product = sp.Id, Limit = 100 },
                    null,
                    cancellationToken))
                {
                    stripePrices.Add(sp2);
                }

                if (stripePrices.Count == 0)
                {
                    continue;
                }

                // Batch-load existing prices for this product in a single query
                var stripePriceIds = stripePrices.Select(p => p.Id).ToList();
                var existingPriceMap = await _dbContext.DainnStripePrices
                    .Where(p => p.ProductId == localProduct.Id
                        && p.StripePriceId != null
                        && stripePriceIds.Contains(p.StripePriceId))
                    .ToDictionaryAsync(p => p.StripePriceId!, cancellationToken);

                foreach (var sp2 in stripePrices)
                {
                    if (existingPriceMap.TryGetValue(sp2.Id, out var existingPrice))
                    {
                        existingPrice.UnitAmount = sp2.UnitAmount ?? 0;
                        existingPrice.Active = sp2.Active;
                        existingPrice.UpdatedAt = now;
                        run.PricesUpdated++;
                    }
                    else
                    {
                        _dbContext.DainnStripePrices.Add(new DainnStripePrice
                        {
                            Id = Guid.NewGuid(),
                            ProductId = localProduct.Id,
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
