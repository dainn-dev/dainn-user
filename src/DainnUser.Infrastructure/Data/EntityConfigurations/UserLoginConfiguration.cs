using DainnUser.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DainnUser.Infrastructure.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for UserLogin entity.
/// </summary>
public class UserLoginConfiguration : IEntityTypeConfiguration<UserLogin>
{
    /// <summary>
    /// Configures the UserLogin entity.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<UserLogin> builder)
    {
        builder.ToTable("UserLogins");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Provider)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(l => l.ProviderKey)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(l => l.ProviderDisplayName)
            .HasMaxLength(256);

        builder.Property(l => l.LinkedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(l => l.UserId);
        builder.HasIndex(l => new { l.Provider, l.ProviderKey })
            .IsUnique();
    }
}
