using DainnStripe.Enums;

namespace DainnStripe.Models;

/// <summary>
/// Request to create or update a managed catalog price.
/// </summary>
public sealed class UpsertCatalogPriceRequest
{
    /// <summary>
    /// Gets or sets the product lookup key.
    /// </summary>
    public string ProductLookupKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the price lookup key.
    /// </summary>
    public string LookupKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe price ID.
    /// </summary>
    public string? StripePriceId { get; set; }

    /// <summary>
    /// Gets or sets the currency.
    /// </summary>
    public string Currency { get; set; } = "usd";

    /// <summary>
    /// Gets or sets the unit amount.
    /// </summary>
    public long UnitAmount { get; set; }

    /// <summary>
    /// Gets or sets the recurring interval.
    /// </summary>
    public DainnStripePriceInterval Interval { get; set; }

    /// <summary>
    /// Gets or sets the interval count.
    /// </summary>
    public int? IntervalCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the price is active.
    /// </summary>
    public bool Active { get; set; } = true;

    /// <summary>
    /// Gets or sets serialized metadata.
    /// </summary>
    public string? MetadataJson { get; set; }
}
