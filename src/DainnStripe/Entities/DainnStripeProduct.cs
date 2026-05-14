namespace DainnStripe.Entities;

/// <summary>
/// Managed catalog product linked to a Stripe product.
/// </summary>
public class DainnStripeProduct
{
    /// <summary>
    /// Gets or sets the row identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the stable application lookup key.
    /// </summary>
    public string LookupKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe product ID.
    /// </summary>
    public string? StripeProductId { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the product is active.
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
    /// Gets or sets product prices.
    /// </summary>
    public ICollection<DainnStripePrice> Prices { get; set; } = new List<DainnStripePrice>();
}
