namespace DainnStripe.Models;

/// <summary>
/// Request to create a Stripe subscription and persist it locally.
/// </summary>
public sealed class CreateSubscriptionRequest
{
    /// <summary>
    /// Gets or sets the application owner/user identifier.
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe Customer ID.
    /// </summary>
    public string StripeCustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe Price ID to subscribe to.
    /// </summary>
    public string StripePriceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional free-trial duration in days.
    /// </summary>
    public int? TrialPeriodDays { get; set; }

    /// <summary>
    /// Gets or sets optional metadata to attach to the Stripe subscription.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
