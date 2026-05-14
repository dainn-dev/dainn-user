namespace DainnStripe.Models;

/// <summary>
/// Request to reconcile local marketplace money movement records with Stripe.
/// </summary>
public sealed class ReconcileMoneyMovementRequest
{
    /// <summary>
    /// Gets or sets the optional owner filter.
    /// </summary>
    public string? OwnerId { get; set; }

    /// <summary>
    /// Gets or sets the optional connected account filter.
    /// </summary>
    public string? StripeAccountId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether transfers should be reconciled.
    /// </summary>
    public bool IncludeTransfers { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether payouts should be reconciled.
    /// </summary>
    public bool IncludePayouts { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether a fresh balance snapshot should be captured.
    /// </summary>
    public bool CaptureBalanceSnapshot { get; set; } = true;
}
