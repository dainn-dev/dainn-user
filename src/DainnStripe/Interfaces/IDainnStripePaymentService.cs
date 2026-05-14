using DainnStripe.Models;

namespace DainnStripe.Interfaces;

/// <summary>
/// Orchestrates PaymentIntent creation and local payment persistence.
/// </summary>
public interface IDainnStripePaymentService
{
    /// <summary>
    /// Creates a PaymentIntent and stores the local pending payment.
    /// </summary>
    Task<PaymentIntentResult> CreatePaymentIntentAsync(
        CreatePaymentIntentRequest request,
        CancellationToken cancellationToken = default);
}
