using DainnStripe.Models;

namespace DainnStripe.Interfaces;

/// <summary>
/// Creates Stripe PaymentIntents behind a testable abstraction.
/// </summary>
public interface IDainnStripePaymentIntentClient
{
    /// <summary>
    /// Creates a Stripe PaymentIntent.
    /// </summary>
    Task<PaymentIntentResult> CreateAsync(
        CreatePaymentIntentRequest request,
        CancellationToken cancellationToken = default);
}
