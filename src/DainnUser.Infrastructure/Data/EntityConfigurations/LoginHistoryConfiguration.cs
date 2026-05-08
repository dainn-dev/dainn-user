using DainnUser.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DainnUser.Infrastructure.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for LoginHistory entity.
/// </summary>
public class LoginHistoryConfiguration : IEntityTypeConfiguration<LoginHistory>
{
    /// <summary>
    /// Configures the LoginHistory entity.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<LoginHistory> builder)
    {
        builder.ToTable("LoginHistories");

        builder.HasKey(lh => lh.Id);

        builder.Property(lh => lh.Provider)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(lh => lh.IsSuccessful)
            .IsRequired();

        builder.Property(lh => lh.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(lh => lh.UserAgent)
            .HasMaxLength(500);

        builder.Property(lh => lh.FailureReason)
            .HasMaxLength(500);

        builder.Property(lh => lh.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(lh => lh.UserId);
        builder.HasIndex(lh => lh.CreatedAt);
        builder.HasIndex(lh => new { lh.UserId, lh.CreatedAt });
    }
}
