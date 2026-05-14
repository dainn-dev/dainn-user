using DainnStripe.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DainnStripe.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for Stripe customer mappings.
/// </summary>
public class StripeCustomerMappingConfiguration : IEntityTypeConfiguration<StripeCustomerMapping>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StripeCustomerMapping> builder)
    {
        builder.ToTable("StripeCustomerMappings");

        builder.HasKey(mapping => mapping.Id);

        builder.Property(mapping => mapping.OwnerId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(mapping => mapping.StripeCustomerId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(mapping => mapping.StripeAccountId)
            .HasMaxLength(128);

        builder.Property(mapping => mapping.Email)
            .HasMaxLength(320);

        builder.Property(mapping => mapping.Name)
            .HasMaxLength(256);

        builder.Property(mapping => mapping.MetadataJson);

        builder.Property(mapping => mapping.CreatedAt)
            .IsRequired();

        builder.Property(mapping => mapping.UpdatedAt)
            .IsRequired();

        builder.HasIndex(mapping => mapping.OwnerId);
        builder.HasIndex(mapping => mapping.StripeCustomerId).IsUnique();
        builder.HasIndex(mapping => new { mapping.OwnerId, mapping.StripeAccountId }).IsUnique();
    }
}
