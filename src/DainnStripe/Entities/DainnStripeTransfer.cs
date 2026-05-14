namespace DainnStripe.Entities;

/// <summary>
/// Local record for a Stripe Transfer to a connected account.
/// </summary>
public class DainnStripeTransfer
{
    /// <summary>
    /// Gets or sets the row identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the connected account row identifier.
    /// </summary>
    public Guid ConnectedAccountId { get; set; }

    /// <summary>
    /// Gets or sets the host application owner identifier.
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe transfer ID.
    /// </summary>
    public string StripeTransferId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe connected account destination.
    /// </summary>
    public string StripeDestinationAccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe source transaction ID.
    /// </summary>
    public string? StripeSourceTransactionId { get; set; }

    /// <summary>
    /// Gets or sets the amount in the smallest currency unit.
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency.
    /// </summary>
    public string Currency { get; set; } = "usd";

    /// <summary>
    /// Gets or sets the transfer group.
    /// </summary>
    public string? TransferGroup { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the transfer has been reversed.
    /// </summary>
    public bool Reversed { get; set; }

    /// <summary>
    /// Gets or sets serialized metadata.
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the connected account.
    /// </summary>
    public DainnStripeConnectedAccount ConnectedAccount { get; set; } = null!;
}
