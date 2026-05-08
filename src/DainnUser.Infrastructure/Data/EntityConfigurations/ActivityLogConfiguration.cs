using DainnUser.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DainnUser.Infrastructure.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for ActivityLog entity.
/// </summary>
public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    /// <summary>
    /// Configures the ActivityLog entity.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("ActivityLogs");

        builder.HasKey(al => al.Id);

        builder.Property(al => al.ActivityType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(al => al.Description)
            .HasMaxLength(1000);

        builder.Property(al => al.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(al => al.UserAgent)
            .HasMaxLength(500);

        builder.Property(al => al.Metadata)
            .HasMaxLength(4000);

        builder.Property(al => al.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(al => al.UserId);
        builder.HasIndex(al => al.ActivityType);
        builder.HasIndex(al => al.CreatedAt);
        builder.HasIndex(al => new { al.UserId, al.ActivityType });
        builder.HasIndex(al => new { al.UserId, al.CreatedAt });
    }
}
