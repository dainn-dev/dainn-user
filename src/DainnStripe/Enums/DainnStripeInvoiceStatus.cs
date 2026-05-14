namespace DainnStripe.Enums;

/// <summary>
/// Local invoice status mirroring Stripe invoice lifecycle.
/// </summary>
public enum DainnStripeInvoiceStatus
{
    /// <summary>Invoice is a draft not yet finalized.</summary>
    Draft = 0,

    /// <summary>Invoice is open and awaiting payment.</summary>
    Open = 1,

    /// <summary>Invoice has been paid.</summary>
    Paid = 2,

    /// <summary>Invoice is uncollectible.</summary>
    Uncollectible = 3,

    /// <summary>Invoice has been voided.</summary>
    Void = 4
}
