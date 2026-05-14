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
    /// Clears the current request context.
    /// </summary>
    void Clear();
}
