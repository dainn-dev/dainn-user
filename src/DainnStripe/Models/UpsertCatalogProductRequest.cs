namespace DainnStripe.Models;

/// <summary>
/// Request to create or update a managed catalog product.
/// </summary>
public sealed class UpsertCatalogProductRequest
{
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
    /// Gets or sets the description.
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
}
