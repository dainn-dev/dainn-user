using DainnUser.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DainnUser.Infrastructure.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for UserContact entity.
/// </summary>
public class UserContactConfiguration : IEntityTypeConfiguration<UserContact>
{
    /// <summary>
    /// Configures the UserContact entity.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<UserContact> builder)
    {
        builder.ToTable("UserContacts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ContactType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.ContactValue)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.IsVerified)
            .IsRequired();

        builder.Property(c => c.IsPrimary)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => new { c.UserId, c.ContactType, c.IsPrimary });
    }
}
