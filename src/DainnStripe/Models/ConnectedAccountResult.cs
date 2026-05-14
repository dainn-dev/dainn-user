namespace DainnStripe.Models;

/// <summary>
/// Result from Stripe connected account creation or retrieval.
/// </summary>
public sealed class ConnectedAccountResult
{
    /// <summary>
    /// Gets or sets the Stripe connected account ID.
    /// </summary>
    public string StripeAccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connected account email.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the country.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Gets or sets the default currency.
    /// </summary>
    public string? DefaultCurrency { get; set; }

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
}
