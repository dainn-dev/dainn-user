namespace DainnStripe.Models;

/// <summary>
/// Request to create or update a DainnStripe tenant.
/// </summary>
public sealed class UpsertTenantRequest
{
    /// <summary>
    /// Gets or sets the stable tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default currency.
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
}
