using DainnStripe.Models;

namespace DainnStripe.Interfaces;

/// <summary>
/// Orchestrates Checkout session creation and local commerce persistence.
/// </summary>
public interface IDainnStripeCheckoutService
{
    /// <summary>
    /// Creates a Checkout session and stores initial local payment or subscription state.
    /// </summary>
    Task<CheckoutSessionResult> CreateAsync(
        CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken = default);
}
