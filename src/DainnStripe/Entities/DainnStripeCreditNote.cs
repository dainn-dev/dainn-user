namespace DainnStripe.Entities;

/// <summary>
/// Local record of a Stripe credit note issued against an invoice.
/// </summary>
public class DainnStripeCreditNote
{
    /// <summary>
    /// Gets or sets the row identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the Stripe CreditNote ID.
    /// </summary>
    public string StripeCreditNoteId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe Invoice ID this credit note belongs to.
    /// </summary>
    public string StripeInvoiceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the application owner identifier.
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the credit amount in the smallest currency unit.
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency.
    /// </summary>
    public string Currency { get; set; } = "usd";

    /// <summary>
    /// Gets or sets the reason for the credit note.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets an optional memo visible on the credit note.
    /// </summary>
    public string? Memo { get; set; }

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
