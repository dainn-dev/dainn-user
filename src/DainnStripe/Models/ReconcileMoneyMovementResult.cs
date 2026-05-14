namespace DainnStripe.Models;

/// <summary>
/// Result from reconciling marketplace money movement records.
/// </summary>
public sealed class ReconcileMoneyMovementResult
{
    /// <summary>
    /// Gets or sets the number of transfers refreshed.
    /// </summary>
    public int TransfersReconciled { get; set; }

    /// <summary>
    /// Gets or sets the number of payouts refreshed.
    /// </summary>
    public int PayoutsReconciled { get; set; }

    /// <summary>
    /// Gets or sets the number of balance snapshots captured.
    /// </summary>
    public int BalanceSnapshotsCaptured { get; set; }
}
