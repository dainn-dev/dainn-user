using DainnStripe.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DainnStripe.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for Stripe balance snapshots.
/// </summary>
public class DainnStripeBalanceSnapshotConfiguration : IEntityTypeConfiguration<DainnStripeBalanceSnapshot>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DainnStripeBalanceSnapshot> builder)
    {
        builder.ToTable("DainnStripeBalanceSnapshots");

        builder.HasKey(snapshot => snapshot.Id);

        builder.Property(snapshot => snapshot.StripeAccountId).HasMaxLength(128);
        builder.Property(snapshot => snapshot.AvailableJson).IsRequired();
        builder.Property(snapshot => snapshot.PendingJson).IsRequired();
        builder.Property(snapshot => snapshot.CreatedAt).IsRequired();
        builder.Property(snapshot => snapshot.UpdatedAt).IsRequired();

        builder.HasOne(snapshot => snapshot.ConnectedAccount)
            .WithMany(account => account.BalanceSnapshots)
            .HasForeignKey(snapshot => snapshot.ConnectedAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(snapshot => snapshot.StripeAccountId);
        builder.HasIndex(snapshot => snapshot.CreatedAt);
    }
}
