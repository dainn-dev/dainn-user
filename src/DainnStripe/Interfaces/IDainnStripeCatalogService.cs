using DainnStripe.Entities;
using DainnStripe.Models;

namespace DainnStripe.Interfaces;

/// <summary>
/// Manages the local Stripe catalog used by application checkout flows.
/// </summary>
public interface IDainnStripeCatalogService
{
    /// <summary>
    /// Creates or updates a managed catalog product by lookup key.
    /// </summary>
    Task<DainnStripeProduct> UpsertProductAsync(
        UpsertCatalogProductRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a managed catalog price by lookup key.
    /// </summary>
    Task<DainnStripePrice> UpsertPriceAsync(
        UpsertCatalogPriceRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active managed catalog products with active prices.
    /// </summary>
    Task<IReadOnlyList<DainnStripeProduct>> GetActiveCatalogAsync(CancellationToken cancellationToken = default);
}
