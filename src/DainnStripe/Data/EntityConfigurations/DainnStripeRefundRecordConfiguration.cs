using DainnStripe.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DainnStripe.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for refund records.
/// </summary>
public class DainnStripeRefundRecordConfiguration : IEntityTypeConfiguration<DainnStripeRefundRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DainnStripeRefundRecord> builder)
    {
        builder.ToTable("DainnStripeRefundRecords");

        builder.HasKey(refund => refund.Id);

        builder.Property(refund => refund.StripeRefundId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(refund => refund.StripeChargeId)
            .HasMaxLength(128);

        builder.Property(refund => refund.StripePaymentIntentId)
            .HasMaxLength(128);

        builder.Property(refund => refund.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(refund => refund.Reason)
            .HasMaxLength(256);

        builder.Property(refund => refund.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(refund => refund.CreatedAt)
            .IsRequired();

        builder.Property(refund => refund.UpdatedAt)
            .IsRequired();

        builder.HasOne(refund => refund.Payment)
            .WithMany(payment => payment.Refunds)
            .HasForeignKey(refund => refund.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(refund => refund.StripeRefundId).IsUnique();
        builder.HasIndex(refund => refund.PaymentId);
        builder.HasIndex(refund => refund.StripePaymentIntentId);
    }
}
