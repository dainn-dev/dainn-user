namespace DainnStripe.Models;

/// <summary>
/// Request to create a Stripe connected account for a tenant owner.
/// </summary>
public sealed class CreateConnectedAccountRequest
{
    /// <summary>
    /// Gets or sets the stable tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the owner identifier within the tenant.
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connected account email.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the account country.
    /// </summary>
    public string Country { get; set; } = "US";

    /// <summary>
    /// Gets or sets the default currency.
    /// </summary>
    public string DefaultCurrency { get; set; } = "usd";

    /// <summary>
    /// Gets metadata passed to Stripe.
    /// </summary>
    public IDictionary<string, string> Metadata { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
}
