using DainnUser.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DainnUser.Infrastructure.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for UserToken entity.
/// </summary>
public class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    /// <summary>
    /// Configures the UserToken entity.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.ToTable("UserTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(t => t.TokenValue)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(t => t.ExpiresAt)
            .IsRequired();

        builder.Property(t => t.IsUsed)
            .IsRequired();

        builder.Property(t => t.IsRevoked)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.TokenValue);
        builder.HasIndex(t => new { t.UserId, t.TokenType });
        builder.HasIndex(t => t.ExpiresAt);
    }
}
