using DainnStripe.Enums;

namespace DainnStripe.Models;

/// <summary>
/// Request to create a Stripe Checkout session.
/// </summary>
public sealed class CreateCheckoutSessionRequest
{
    /// <summary>
    /// Gets or sets the owner/user identifier.
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the checkout mode.
    /// </summary>
    public DainnStripeCheckoutMode Mode { get; set; }

    /// <summary>
    /// Gets or sets the Stripe customer ID.
    /// </summary>
    public string? StripeCustomerId { get; set; }

    /// <summary>
    /// Gets or sets the success URL.
    /// </summary>
    public string SuccessUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cancel URL.
    /// </summary>
    public string CancelUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets checkout line items.
    /// </summary>
    public IList<CreateCheckoutSessionLineItem> LineItems { get; } = new List<CreateCheckoutSessionLineItem>();

    /// <summary>
    /// Gets metadata passed to Stripe Checkout.
    /// </summary>
    public IDictionary<string, string> Metadata { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
}
