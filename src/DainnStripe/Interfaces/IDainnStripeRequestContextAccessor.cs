using DainnStripe.Models;

namespace DainnStripe.Interfaces;

/// <summary>
/// Stores Stripe request context for the current dependency injection scope.
/// </summary>
public interface IDainnStripeRequestContextAccessor
{
    /// <summary>
    /// Gets the current request context.
    /// </summary>
    DainnStripeRequestContext Current { get; }

    /// <summary>
    /// Sets the current connected account context.
    /// </summary>
    void SetConnectedAccount(string stripeAccountId, string? tenantId = null);

    /// <summary>
    /// Sets an idempotency key that will be forwarded as the <c>Idempotency-Key</c> header
    /// on the next Stripe write operation performed within this scope.
    /// </summary>
    void SetIdempotencyKey(string idempotencyKey);

    /// <summary>
    /// Clears the current request context, including any idempotency key.
    /// </summary>
    void Clear();
}
