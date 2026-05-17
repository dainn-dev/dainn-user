using DainnStripe.Entities;

namespace DainnStripe.Models;

/// <summary>
/// A single page of active catalog products returned by <see cref="Interfaces.IDainnStripeCatalogService.GetActiveCatalogPagedAsync"/>.
/// </summary>
public sealed class CatalogPage
{
    /// <summary>
    /// Gets the products on this page, each including their active prices.
    /// </summary>
    public IReadOnlyList<DainnStripeProduct> Items { get; init; } = Array.Empty<DainnStripeProduct>();

    /// <summary>
    /// Gets the 1-based page number.
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Gets the maximum number of items per page.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Gets the total number of active products across all pages.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Gets a value indicating whether a next page exists.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Gets a value indicating whether a previous page exists.
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}
