using DainnStripe.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DainnStripe.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for SaaS tenants.
/// </summary>
public class DainnStripeTenantConfiguration : IEntityTypeConfiguration<DainnStripeTenant>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DainnStripeTenant> builder)
    {
        builder.ToTable("DainnStripeTenants");

        builder.HasKey(tenant => tenant.Id);

        builder.Property(tenant => tenant.TenantId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(tenant => tenant.DisplayName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(tenant => tenant.DefaultCurrency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(tenant => tenant.CreatedAt).IsRequired();
        builder.Property(tenant => tenant.UpdatedAt).IsRequired();

        builder.HasIndex(tenant => tenant.TenantId).IsUnique();
    }
}
