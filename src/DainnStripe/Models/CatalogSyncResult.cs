namespace DainnStripe.Models;

/// <summary>
/// Summary of a completed catalog sync run.
/// </summary>
public sealed class CatalogSyncResult
{
    /// <summary>
    /// Gets or sets the sync run identifier.
    /// </summary>
    public Guid SyncRunId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the sync succeeded.
    /// </summary>
    public bool Succeeded { get; set; }

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
}
