using DainnStripe.Enums;

namespace DainnStripe.Models;

/// <summary>
/// Request to sync connected account status from Stripe.
/// </summary>
public sealed class SyncConnectedAccountRequest
{
    /// <summary>
    /// Gets or sets the Stripe connected account ID.
    /// </summary>
    public string StripeAccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether charges are enabled.
    /// </summary>
    public bool ChargesEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether payouts are enabled.
    /// </summary>
    public bool PayoutsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether details are submitted.
    /// </summary>
    public bool DetailsSubmitted { get; set; }

    /// <summary>
    /// Gets or sets an optional explicit local status.
    /// </summary>
    public DainnStripeConnectedAccountStatus? Status { get; set; }
}
