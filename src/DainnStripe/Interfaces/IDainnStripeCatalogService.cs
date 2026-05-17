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
    /// When <see cref="UpsertCatalogProductRequest.StripeProductId"/> is <see langword="null"/>,
    /// a Stripe Product is created automatically and the ID is persisted locally.
    /// </summary>
    Task<DainnStripeProduct> UpsertProductAsync(
        UpsertCatalogProductRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a managed catalog price by lookup key.
    /// When <see cref="UpsertCatalogPriceRequest.StripePriceId"/> is <see langword="null"/>,
    /// a Stripe Price is created automatically and the ID is persisted locally.
    /// </summary>
    Task<DainnStripePrice> UpsertPriceAsync(
        UpsertCatalogPriceRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active managed catalog products with their active prices.
    /// Suitable for small catalogs only. Use <see cref="GetActiveCatalogPagedAsync"/> for large catalogs.
    /// </summary>
    Task<IReadOnlyList<DainnStripeProduct>> GetActiveCatalogAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated page of active managed catalog products with their active prices.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page (1–200, default 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<CatalogPage> GetActiveCatalogPagedAsync(
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
}
