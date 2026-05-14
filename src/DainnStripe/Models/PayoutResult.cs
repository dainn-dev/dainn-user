namespace DainnStripe.Models;

/// <summary>
/// Result from creating a Stripe Payout.
/// </summary>
public sealed class PayoutResult
{
    /// <summary>
    /// Gets or sets the Stripe payout ID.
    /// </summary>
    public string PayoutId { get; set; } = string.Empty;

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
}
