namespace DainnStripe.Entities;

/// <summary>
/// Local record for a Stripe Payout from a connected account balance.
/// </summary>
public class DainnStripePayout
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
    /// Gets or sets the Stripe payout ID.
    /// </summary>
    public string StripePayoutId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe connected account ID where the payout was created.
    /// </summary>
    public string StripeAccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the amount in the smallest currency unit.
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency.
    /// </summary>
    public string Currency { get; set; } = "usd";

    /// <summary>
    /// Gets or sets the payout destination.
    /// </summary>
    public string? Destination { get; set; }

    /// <summary>
    /// Gets or sets the payout method.
    /// </summary>
    public string? Method { get; set; }

    /// <summary>
    /// Gets or sets the Stripe payout status.
    /// </summary>
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Gets or sets the expected arrival date.
    /// </summary>
    public DateTime? ArrivalDate { get; set; }

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
