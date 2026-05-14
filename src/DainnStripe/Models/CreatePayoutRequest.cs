namespace DainnStripe.Models;

/// <summary>
/// Request to create a Stripe Payout for a connected account.
/// </summary>
public sealed class CreatePayoutRequest
{
    /// <summary>
    /// Gets or sets the host application owner identifier.
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe connected account where the payout should be created.
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
    /// Gets or sets the external account or card destination.
    /// </summary>
    public string? Destination { get; set; }

    /// <summary>
    /// Gets or sets the payout method, such as standard or instant.
    /// </summary>
    public string? Method { get; set; }

    /// <summary>
    /// Gets or sets the payout description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the statement descriptor.
    /// </summary>
    public string? StatementDescriptor { get; set; }

    /// <summary>
    /// Gets or sets an optional Stripe idempotency key.
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Gets metadata passed to Stripe.
    /// </summary>
    public IDictionary<string, string> Metadata { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
}
