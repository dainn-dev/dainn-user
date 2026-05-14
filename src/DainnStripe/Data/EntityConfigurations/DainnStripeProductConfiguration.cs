using DainnStripe.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DainnStripe.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for managed catalog products.
/// </summary>
public class DainnStripeProductConfiguration : IEntityTypeConfiguration<DainnStripeProduct>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DainnStripeProduct> builder)
    {
        builder.ToTable("DainnStripeProducts");

        builder.HasKey(product => product.Id);

        builder.Property(product => product.LookupKey)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(product => product.StripeProductId)
            .HasMaxLength(128);

        builder.Property(product => product.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(product => product.Description)
            .HasMaxLength(2048);

        builder.Property(product => product.CreatedAt)
            .IsRequired();

        builder.Property(product => product.UpdatedAt)
            .IsRequired();

        builder.HasIndex(product => product.LookupKey).IsUnique();
        builder.HasIndex(product => product.StripeProductId).IsUnique();
    }
}
