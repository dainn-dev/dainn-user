namespace DainnStripe.Entities;

/// <summary>
/// Maps an application owner/user to a Stripe customer.
/// </summary>
public class StripeCustomerMapping
{
    /// <summary>
    /// Gets or sets the row identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the application owner identifier. This is intentionally a string so hosts can use GUIDs,
    /// integers, tenant IDs, or composite external IDs.
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe customer identifier.
    /// </summary>
    public string StripeCustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe account identifier for Connect scenarios.
    /// </summary>
    public string? StripeAccountId { get; set; }

    /// <summary>
    /// Gets or sets the customer email snapshot.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the customer name snapshot.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the customer belongs to live mode.
    /// </summary>
    public bool Livemode { get; set; }

    /// <summary>
    /// Gets or sets serialized Stripe metadata.
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
}
