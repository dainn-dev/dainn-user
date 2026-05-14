using DainnStripe.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DainnStripe.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for managed catalog prices.
/// </summary>
public class DainnStripePriceConfiguration : IEntityTypeConfiguration<DainnStripePrice>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DainnStripePrice> builder)
    {
        builder.ToTable("DainnStripePrices");

        builder.HasKey(price => price.Id);

        builder.Property(price => price.LookupKey)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(price => price.StripePriceId)
            .HasMaxLength(128);

        builder.Property(price => price.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(price => price.Interval)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(price => price.BillingScheme)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(price => price.CreatedAt)
            .IsRequired();

        builder.Property(price => price.UpdatedAt)
            .IsRequired();

        builder.HasOne(price => price.Product)
            .WithMany(product => product.Prices)
            .HasForeignKey(price => price.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(price => price.LookupKey).IsUnique();
        builder.HasIndex(price => price.StripePriceId).IsUnique();
        builder.HasIndex(price => price.ProductId);
    }
}
