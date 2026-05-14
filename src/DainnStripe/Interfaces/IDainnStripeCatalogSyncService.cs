using DainnStripe.Models;

namespace DainnStripe.Interfaces;

/// <summary>
/// Syncs the local managed catalog from Stripe products and prices.
/// </summary>
public interface IDainnStripeCatalogSyncService
{
    /// <summary>
    /// Pulls active products and prices from Stripe and upserts them into the local catalog.
    /// Creates a <see cref="DainnStripe.Entities.DainnStripeCatalogSyncRun"/> audit record for each run.
    /// </summary>
    Task<CatalogSyncResult> SyncFromStripeAsync(
        string? triggerSource = null,
        CancellationToken cancellationToken = default);
}
