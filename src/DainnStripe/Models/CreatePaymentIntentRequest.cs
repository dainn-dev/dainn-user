namespace DainnStripe.Models;

/// <summary>
/// Request to create a Stripe PaymentIntent.
/// </summary>
public sealed class CreatePaymentIntentRequest
{
    /// <summary>
    /// Gets or sets the owner/user identifier.
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the amount in the smallest currency unit.
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency.
    /// </summary>
    public string Currency { get; set; } = "usd";

    /// <summary>
    /// Gets or sets the Stripe customer ID.
    /// </summary>
    public string? StripeCustomerId { get; set; }

    /// <summary>
    /// Gets or sets the payment description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the application fee amount for Connect destination charges.
    /// </summary>
    public long? ApplicationFeeAmount { get; set; }

    /// <summary>
    /// Gets or sets the connected account destination for automatic transfer data.
    /// </summary>
    public string? TransferDestinationAccountId { get; set; }

    /// <summary>
    /// Gets or sets the transfer amount for automatic transfer data.
    /// </summary>
    public long? TransferAmount { get; set; }

    /// <summary>
    /// Gets or sets the transfer group.
    /// </summary>
    public string? TransferGroup { get; set; }

    /// <summary>
    /// Gets metadata passed to Stripe.
    /// </summary>
    public IDictionary<string, string> Metadata { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
}
