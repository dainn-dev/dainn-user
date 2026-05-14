using DainnStripe.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DainnStripe.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for payment attempt records.
/// </summary>
public class DainnStripePaymentAttemptConfiguration : IEntityTypeConfiguration<DainnStripePaymentAttempt>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DainnStripePaymentAttempt> builder)
    {
        builder.ToTable("DainnStripePaymentAttempts");

        builder.HasKey(attempt => attempt.Id);

        builder.Property(attempt => attempt.StripePaymentIntentId)
            .HasMaxLength(128);

        builder.Property(attempt => attempt.StripeChargeId)
            .HasMaxLength(128);

        builder.Property(attempt => attempt.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(attempt => attempt.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(attempt => attempt.ErrorCode)
            .HasMaxLength(64);

        builder.Property(attempt => attempt.ErrorMessage)
            .HasMaxLength(1024);

        builder.Property(attempt => attempt.CreatedAt)
            .IsRequired();

        builder.HasOne(attempt => attempt.Payment)
            .WithMany(payment => payment.Attempts)
            .HasForeignKey(attempt => attempt.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(attempt => attempt.PaymentId);
        builder.HasIndex(attempt => attempt.StripePaymentIntentId);
        builder.HasIndex(attempt => attempt.StripeChargeId);
    }
}
