namespace DainnStripe.Models;

/// <summary>
/// Result from creating a Stripe Transfer.
/// </summary>
public sealed class TransferResult
{
    /// <summary>
    /// Gets or sets the Stripe transfer ID.
    /// </summary>
    public string TransferId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe connected account destination.
    /// </summary>
    public string DestinationAccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe source transaction ID.
    /// </summary>
    public string? SourceTransactionId { get; set; }

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
    /// Gets or sets a value indicating whether the transfer is reversed.
    /// </summary>
    public bool Reversed { get; set; }
}
