using DainnStripe.Enums;

namespace DainnStripe.Entities;

/// <summary>
/// Managed catalog price linked to a Stripe price.
/// </summary>
public class DainnStripePrice
{
    /// <summary>
    /// Gets or sets the row identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the product identifier.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the stable application lookup key.
    /// </summary>
    public string LookupKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe price ID.
    /// </summary>
    public string? StripePriceId { get; set; }

    /// <summary>
    /// Gets or sets the price currency.
    /// </summary>
    public string Currency { get; set; } = "usd";

    /// <summary>
    /// Gets or sets the unit amount in the smallest currency unit.
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
    /// Gets or sets the billing scheme.
    /// </summary>
    public DainnStripePriceBillingScheme BillingScheme { get; set; } = DainnStripePriceBillingScheme.PerUnit;

    /// <summary>
    /// Gets or sets a value indicating whether the price is active.
    /// </summary>
    public bool Active { get; set; } = true;

    /// <summary>
    /// Gets or sets serialized metadata.
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the product.
    /// </summary>
    public DainnStripeProduct Product { get; set; } = null!;
}
