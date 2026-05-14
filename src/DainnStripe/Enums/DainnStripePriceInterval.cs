namespace DainnStripe.Enums;

/// <summary>
/// Recurring interval for a managed Stripe price.
/// </summary>
public enum DainnStripePriceInterval
{
    /// <summary>
    /// Price is not recurring.
    /// </summary>
    None = 0,

    /// <summary>
    /// Daily interval.
    /// </summary>
    Day = 1,

    /// <summary>
    /// Weekly interval.
    /// </summary>
    Week = 2,

    /// <summary>
    /// Monthly interval.
    /// </summary>
    Month = 3,

    /// <summary>
    /// Yearly interval.
    /// </summary>
    Year = 4
}
