using DainnStripe.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DainnStripe.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for Stripe payouts.
/// </summary>
public class DainnStripePayoutConfiguration : IEntityTypeConfiguration<DainnStripePayout>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DainnStripePayout> builder)
    {
        builder.ToTable("DainnStripePayouts");

        builder.HasKey(payout => payout.Id);

        builder.Property(payout => payout.OwnerId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(payout => payout.StripePayoutId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(payout => payout.StripeAccountId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(payout => payout.Currency).IsRequired().HasMaxLength(3);
        builder.Property(payout => payout.Destination).HasMaxLength(128);
        builder.Property(payout => payout.Method).HasMaxLength(32);
        builder.Property(payout => payout.Status).IsRequired().HasMaxLength(32);
        builder.Property(payout => payout.CreatedAt).IsRequired();
        builder.Property(payout => payout.UpdatedAt).IsRequired();

        builder.HasOne(payout => payout.ConnectedAccount)
            .WithMany(account => account.Payouts)
            .HasForeignKey(payout => payout.ConnectedAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(payout => payout.OwnerId);
        builder.HasIndex(payout => payout.StripePayoutId).IsUnique();
        builder.HasIndex(payout => payout.StripeAccountId);
    }
}
