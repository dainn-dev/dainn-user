namespace DainnStripe.Entities;

/// <summary>
/// SaaS tenant record for DainnStripe Connect scenarios.
/// </summary>
public class DainnStripeTenant
{
    /// <summary>
    /// Gets or sets the row identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the stable tenant identifier supplied by the host application.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default currency for tenant commerce.
    /// </summary>
    public string DefaultCurrency { get; set; } = "usd";

    /// <summary>
    /// Gets or sets a value indicating whether the tenant is active.
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
    /// Gets or sets connected accounts owned by this tenant.
    /// </summary>
    public ICollection<DainnStripeConnectedAccount> ConnectedAccounts { get; set; } = new List<DainnStripeConnectedAccount>();
}
