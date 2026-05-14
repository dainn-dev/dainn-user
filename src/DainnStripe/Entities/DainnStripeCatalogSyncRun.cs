namespace DainnStripe.Entities;

/// <summary>
/// Audit record for a single catalog sync run from Stripe.
/// </summary>
public class DainnStripeCatalogSyncRun
{
    /// <summary>
    /// Gets or sets the row identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets what triggered this sync (e.g., "manual", "webhook", "scheduled").
    /// </summary>
    public string TriggerSource { get; set; } = "manual";

    /// <summary>
    /// Gets or sets the number of products created.
    /// </summary>
    public int ProductsCreated { get; set; }

    /// <summary>
    /// Gets or sets the number of products updated.
    /// </summary>
    public int ProductsUpdated { get; set; }

    /// <summary>
    /// Gets or sets the number of prices created.
    /// </summary>
    public int PricesCreated { get; set; }

    /// <summary>
    /// Gets or sets the number of prices updated.
    /// </summary>
    public int PricesUpdated { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the sync completed without error.
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// Gets or sets the error message if the sync failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets when the sync started.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the sync completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}
