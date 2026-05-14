namespace DainnStripe.Enums;

/// <summary>
/// Supported Stripe Checkout modes.
/// </summary>
public enum DainnStripeCheckoutMode
{
    /// <summary>
    /// One-time payment mode.
    /// </summary>
    Payment = 0,

    /// <summary>
    /// Recurring subscription mode.
    /// </summary>
    Subscription = 1
}
