using Stripe;

namespace DainnStripe.Interfaces;

/// <summary>
/// Creates Stripe request options from the current DainnStripe request context.
/// </summary>
public interface IDainnStripeRequestOptionsFactory
{
    /// <summary>
    /// Creates request options for Stripe.net calls.
    /// </summary>
    RequestOptions? Create();
}
