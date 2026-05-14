using Stripe;

namespace DainnStripe.Interfaces;

/// <summary>
/// Creates configured Stripe API clients.
/// </summary>
public interface IDainnStripeClientFactory
{
    /// <summary>
    /// Creates a Stripe client using the configured secret key.
    /// </summary>
    IStripeClient CreateClient();
}
