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

    /// <summary>
    /// Gets or sets the idempotency key forwarded as the <c>Idempotency-Key</c> header on
    /// Stripe write operations. Prevents duplicate execution when a request is retried.
    /// </summary>
    public string? IdempotencyKey { get; set; }
}
