namespace DainnStripe.Models;

/// <summary>
/// Scoped Stripe request context used for connected account operations.
/// </summary>
public sealed class DainnStripeRequestContext
{
    /// <summary>
    /// Gets or sets the host tenant identifier.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the Stripe connected account ID used for scoped Stripe API calls.
    /// </summary>
    public string? StripeAccountId { get; set; }
}
