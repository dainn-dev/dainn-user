using DainnStripe.Entities;
using Microsoft.EntityFrameworkCore;

namespace DainnStripe.Data;

/// <summary>
/// Database context for DainnStripe persistence.
/// </summary>
public class DainnStripeDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeDbContext"/> class.
    /// </summary>
    public DainnStripeDbContext(DbContextOptions<DainnStripeDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets Stripe customer mappings.
    /// </summary>
    public DbSet<StripeCustomerMapping> StripeCustomerMappings { get; set; } = null!;

    /// <summary>
    /// Gets or sets Stripe webhook event inbox records.
    /// </summary>
    public DbSet<StripeWebhookEventRecord> StripeWebhookEvents { get; set; } = null!;

    /// <summary>
    /// Gets or sets managed catalog products.
    /// </summary>
    public DbSet<DainnStripeProduct> DainnStripeProducts { get; set; } = null!;

    /// <summary>
    /// Gets or sets managed catalog prices.
    /// </summary>
    public DbSet<DainnStripePrice> DainnStripePrices { get; set; } = null!;

    /// <summary>
    /// Gets or sets local payment records.
    /// </summary>
    public DbSet<DainnStripePayment> DainnStripePayments { get; set; } = null!;

    /// <summary>
    /// Gets or sets local subscription records.
    /// </summary>
    public DbSet<DainnStripeSubscription> DainnStripeSubscriptions { get; set; } = null!;

    /// <summary>
    /// Gets or sets SaaS tenants.
    /// </summary>
    public DbSet<DainnStripeTenant> DainnStripeTenants { get; set; } = null!;

    /// <summary>
    /// Gets or sets Stripe connected accounts.
    /// </summary>
    public DbSet<DainnStripeConnectedAccount> DainnStripeConnectedAccounts { get; set; } = null!;

    /// <summary>
    /// Gets or sets Stripe transfers.
    /// </summary>
    public DbSet<DainnStripeTransfer> DainnStripeTransfers { get; set; } = null!;

    /// <summary>
    /// Gets or sets Stripe payouts.
    /// </summary>
    public DbSet<DainnStripePayout> DainnStripePayouts { get; set; } = null!;

    /// <summary>
    /// Gets or sets Stripe balance snapshots.
    /// </summary>
    public DbSet<DainnStripeBalanceSnapshot> DainnStripeBalanceSnapshots { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DainnStripeDbContext).Assembly);
    }
}
