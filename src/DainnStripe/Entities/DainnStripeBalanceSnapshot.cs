namespace DainnStripe.Entities;

/// <summary>
/// Local snapshot of Stripe balance state for the platform or a connected account.
/// </summary>
public class DainnStripeBalanceSnapshot
{
    /// <summary>
    /// Gets or sets the row identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the connected account row identifier when the snapshot is account-scoped.
    /// </summary>
    public Guid? ConnectedAccountId { get; set; }

    /// <summary>
    /// Gets or sets the Stripe account identifier. Null means the platform account.
    /// </summary>
    public string? StripeAccountId { get; set; }

    /// <summary>
    /// Gets or sets serialized available balance amounts.
    /// </summary>
    public string AvailableJson { get; set; } = "[]";

    /// <summary>
    /// Gets or sets serialized pending balance amounts.
    /// </summary>
    public string PendingJson { get; set; } = "[]";

    /// <summary>
    /// Gets or sets a value indicating whether the snapshot came from live mode.
    /// </summary>
    public bool Livemode { get; set; }

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
    public DainnStripeConnectedAccount? ConnectedAccount { get; set; }
}
