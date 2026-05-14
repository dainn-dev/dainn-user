using DainnStripe.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DainnStripe.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for Stripe connected accounts.
/// </summary>
public class DainnStripeConnectedAccountConfiguration : IEntityTypeConfiguration<DainnStripeConnectedAccount>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DainnStripeConnectedAccount> builder)
    {
        builder.ToTable("DainnStripeConnectedAccounts");

        builder.HasKey(account => account.Id);

        builder.Property(account => account.OwnerId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(account => account.StripeAccountId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(account => account.Email).HasMaxLength(320);
        builder.Property(account => account.Country).HasMaxLength(2);
        builder.Property(account => account.DefaultCurrency).HasMaxLength(3);
        builder.Property(account => account.OnboardingUrl).HasMaxLength(2048);

        builder.Property(account => account.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(account => account.CreatedAt).IsRequired();
        builder.Property(account => account.UpdatedAt).IsRequired();

        builder.HasOne(account => account.Tenant)
            .WithMany(tenant => tenant.ConnectedAccounts)
            .HasForeignKey(account => account.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(account => account.OwnerId);
        builder.HasIndex(account => account.StripeAccountId).IsUnique();
        builder.HasIndex(account => new { account.TenantId, account.OwnerId }).IsUnique();
    }
}
