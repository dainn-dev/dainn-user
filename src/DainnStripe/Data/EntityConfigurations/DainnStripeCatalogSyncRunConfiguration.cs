using DainnStripe.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DainnStripe.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for catalog sync run audit records.
/// </summary>
public class DainnStripeCatalogSyncRunConfiguration : IEntityTypeConfiguration<DainnStripeCatalogSyncRun>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DainnStripeCatalogSyncRun> builder)
    {
        builder.ToTable("DainnStripeCatalogSyncRuns");

        builder.HasKey(run => run.Id);

        builder.Property(run => run.TriggerSource)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(run => run.ErrorMessage)
            .HasMaxLength(2048);

        builder.Property(run => run.StartedAt)
            .IsRequired();
    }
}
