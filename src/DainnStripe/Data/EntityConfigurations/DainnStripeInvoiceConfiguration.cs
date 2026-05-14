using DainnStripe.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DainnStripe.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for invoice records.
/// </summary>
public class DainnStripeInvoiceConfiguration : IEntityTypeConfiguration<DainnStripeInvoice>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DainnStripeInvoice> builder)
    {
        builder.ToTable("DainnStripeInvoices");

        builder.HasKey(invoice => invoice.Id);

        builder.Property(invoice => invoice.StripeInvoiceId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(invoice => invoice.StripeSubscriptionId)
            .HasMaxLength(128);

        builder.Property(invoice => invoice.OwnerId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(invoice => invoice.StripeCustomerId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(invoice => invoice.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(invoice => invoice.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(invoice => invoice.HostedInvoiceUrl)
            .HasMaxLength(2048);

        builder.Property(invoice => invoice.InvoicePdfUrl)
            .HasMaxLength(2048);

        builder.Property(invoice => invoice.CreatedAt)
            .IsRequired();

        builder.Property(invoice => invoice.UpdatedAt)
            .IsRequired();

        builder.HasIndex(invoice => invoice.StripeInvoiceId).IsUnique();
        builder.HasIndex(invoice => invoice.StripeSubscriptionId);
        builder.HasIndex(invoice => invoice.OwnerId);
        builder.HasIndex(invoice => invoice.StripeCustomerId);
    }
}
