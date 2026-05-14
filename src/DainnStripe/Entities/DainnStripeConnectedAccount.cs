using DainnStripe.Enums;

namespace DainnStripe.Entities;

/// <summary>
/// Local representation of a Stripe connected account.
/// </summary>
public class DainnStripeConnectedAccount
{
    /// <summary>
    /// Gets or sets the row identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant row identifier.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the host application account owner identifier.
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe connected account ID.
    /// </summary>
    public string StripeAccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connected account email snapshot.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the account country.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Gets or sets the default currency.
    /// </summary>
    public string? DefaultCurrency { get; set; }

    /// <summary>
    /// Gets or sets the local connected account status.
    /// </summary>
    public DainnStripeConnectedAccountStatus Status { get; set; } = DainnStripeConnectedAccountStatus.OnboardingRequired;

    /// <summary>
    /// Gets or sets a value indicating whether Stripe charges are enabled.
    /// </summary>
    public bool ChargesEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Stripe payouts are enabled.
    /// </summary>
    public bool PayoutsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether account details were submitted.
    /// </summary>
    public bool DetailsSubmitted { get; set; }

    /// <summary>
    /// Gets or sets the latest onboarding link URL.
    /// </summary>
    public string? OnboardingUrl { get; set; }

    /// <summary>
    /// Gets or sets when the latest onboarding link expires.
    /// </summary>
    public DateTime? OnboardingUrlExpiresAt { get; set; }

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

    /// <summary>
    /// Gets or sets the tenant.
    /// </summary>
    public DainnStripeTenant Tenant { get; set; } = null!;

    /// <summary>
    /// Gets or sets transfers sent to this connected account.
    /// </summary>
    public ICollection<DainnStripeTransfer> Transfers { get; set; } = new List<DainnStripeTransfer>();

    /// <summary>
    /// Gets or sets payouts created for this connected account.
    /// </summary>
    public ICollection<DainnStripePayout> Payouts { get; set; } = new List<DainnStripePayout>();

    /// <summary>
    /// Gets or sets balance snapshots for this connected account.
    /// </summary>
    public ICollection<DainnStripeBalanceSnapshot> BalanceSnapshots { get; set; } = new List<DainnStripeBalanceSnapshot>();
}
