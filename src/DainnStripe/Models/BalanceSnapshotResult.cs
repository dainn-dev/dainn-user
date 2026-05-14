namespace DainnStripe.Models;

/// <summary>
/// Balance snapshot data from Stripe.
/// </summary>
public sealed class BalanceSnapshotResult
{
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
}
