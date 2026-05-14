namespace DainnStripe.Enums;

/// <summary>
/// Billing scheme for a managed Stripe price.
/// </summary>
public enum DainnStripePriceBillingScheme
{
    /// <summary>
    /// Per-unit billing.
    /// </summary>
    PerUnit = 0,

    /// <summary>
    /// Tiered billing.
    /// </summary>
    Tiered = 1
}
