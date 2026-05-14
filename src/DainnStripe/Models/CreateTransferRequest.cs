namespace DainnStripe.Models;

/// <summary>
/// Request to create a Stripe Transfer to a connected account.
/// </summary>
public sealed class CreateTransferRequest
{
    /// <summary>
    /// Gets or sets the host application owner identifier.
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe connected account destination.
    /// </summary>
    public string StripeDestinationAccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the amount in the smallest currency unit.
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency.
    /// </summary>
    public string Currency { get; set; } = "usd";

    /// <summary>
    /// Gets or sets the Stripe charge or payment source transaction ID.
    /// </summary>
    public string? StripeSourceTransactionId { get; set; }

    /// <summary>
    /// Gets or sets the transfer group.
    /// </summary>
    public string? TransferGroup { get; set; }

    /// <summary>
    /// Gets or sets the transfer description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets an optional Stripe idempotency key.
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Gets metadata passed to Stripe.
    /// </summary>
    public IDictionary<string, string> Metadata { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
}
