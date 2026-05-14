using DainnStripe.Enums;

namespace DainnStripe.Entities;

/// <summary>
/// Local record of a Stripe invoice associated with a subscription.
/// </summary>
public class DainnStripeInvoice
{
    /// <summary>
    /// Gets or sets the row identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the Stripe Invoice ID.
    /// </summary>
    public string StripeInvoiceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe Subscription ID this invoice belongs to.
    /// </summary>
    public string? StripeSubscriptionId { get; set; }

    /// <summary>
    /// Gets or sets the application owner identifier resolved via customer mapping.
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe Customer ID.
    /// </summary>
    public string StripeCustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the amount due in the smallest currency unit.
    /// </summary>
    public long AmountDue { get; set; }

    /// <summary>
    /// Gets or sets the amount paid in the smallest currency unit.
    /// </summary>
    public long AmountPaid { get; set; }

    /// <summary>
    /// Gets or sets the currency.
    /// </summary>
    public string Currency { get; set; } = "usd";

    /// <summary>
    /// Gets or sets the invoice status.
    /// </summary>
    public DainnStripeInvoiceStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the hosted invoice URL.
    /// </summary>
    public string? HostedInvoiceUrl { get; set; }

    /// <summary>
    /// Gets or sets the invoice PDF URL.
    /// </summary>
    public string? InvoicePdfUrl { get; set; }

    /// <summary>
    /// Gets or sets the billing period start.
    /// </summary>
    public DateTime? PeriodStart { get; set; }

    /// <summary>
    /// Gets or sets the billing period end.
    /// </summary>
    public DateTime? PeriodEnd { get; set; }

    /// <summary>
    /// Gets or sets serialized metadata.
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
