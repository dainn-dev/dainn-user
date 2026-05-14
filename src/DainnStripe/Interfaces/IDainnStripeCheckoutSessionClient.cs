using DainnStripe.Models;

namespace DainnStripe.Interfaces;

/// <summary>
/// Creates Stripe Checkout sessions behind a testable abstraction.
/// </summary>
public interface IDainnStripeCheckoutSessionClient
{
    /// <summary>
    /// Creates a Stripe Checkout session.
    /// </summary>
    Task<CheckoutSessionResult> CreateAsync(
        CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken = default);
}
