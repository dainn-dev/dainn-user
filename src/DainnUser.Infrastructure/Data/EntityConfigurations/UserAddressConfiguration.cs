using DainnUser.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DainnUser.Infrastructure.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for UserAddress entity.
/// </summary>
public class UserAddressConfiguration : IEntityTypeConfiguration<UserAddress>
{
    /// <summary>
    /// Configures the UserAddress entity.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<UserAddress> builder)
    {
        builder.ToTable("UserAddresses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AddressType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.AddressLine1)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.AddressLine2)
            .HasMaxLength(500);

        builder.Property(a => a.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.StateProvince)
            .HasMaxLength(100);

        builder.Property(a => a.PostalCode)
            .HasMaxLength(20);

        builder.Property(a => a.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.IsDefault)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => new { a.UserId, a.IsDefault });
    }
}
