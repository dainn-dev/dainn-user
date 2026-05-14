namespace DainnStripe.Models;

/// <summary>
/// Checkout session line item.
/// </summary>
public sealed class CreateCheckoutSessionLineItem
{
    /// <summary>
    /// Gets or sets the Stripe price ID.
    /// </summary>
    public string StripePriceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    public long Quantity { get; set; } = 1;

    /// <summary>
    /// Gets or sets the unit amount for local payment persistence.
    /// </summary>
    public long? UnitAmount { get; set; }

    /// <summary>
    /// Gets or sets the currency for local payment persistence.
    /// </summary>
    public string? Currency { get; set; }
}
