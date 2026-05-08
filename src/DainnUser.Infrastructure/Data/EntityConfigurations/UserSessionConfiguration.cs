using DainnUser.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DainnUser.Infrastructure.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for UserSession entity.
/// </summary>
public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    /// <summary>
    /// Configures the UserSession entity.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SessionToken)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(s => s.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(s => s.UserAgent)
            .HasMaxLength(500);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.ExpiresAt)
            .IsRequired();

        builder.Property(s => s.LastActivityAt)
            .IsRequired();

        builder.Property(s => s.IsActive)
            .IsRequired();

        // Indexes
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.SessionToken)
            .IsUnique();
        builder.HasIndex(s => s.ExpiresAt);
        builder.HasIndex(s => new { s.UserId, s.IsActive });
    }
}
