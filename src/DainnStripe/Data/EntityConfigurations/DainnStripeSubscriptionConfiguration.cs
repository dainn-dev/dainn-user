using DainnStripe.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DainnStripe.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for local subscription records.
/// </summary>
public class DainnStripeSubscriptionConfiguration : IEntityTypeConfiguration<DainnStripeSubscription>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DainnStripeSubscription> builder)
    {
        builder.ToTable("DainnStripeSubscriptions");

        builder.HasKey(subscription => subscription.Id);

        builder.Property(subscription => subscription.OwnerId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(subscription => subscription.StripeSubscriptionId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(subscription => subscription.StripeCustomerId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(subscription => subscription.StripePriceId)
            .HasMaxLength(128);

        builder.Property(subscription => subscription.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(subscription => subscription.CreatedAt)
            .IsRequired();

        builder.Property(subscription => subscription.UpdatedAt)
            .IsRequired();

        builder.HasIndex(subscription => subscription.OwnerId);
        builder.HasIndex(subscription => subscription.StripeSubscriptionId).IsUnique();
        builder.HasIndex(subscription => subscription.StripeCustomerId);
    }
}
