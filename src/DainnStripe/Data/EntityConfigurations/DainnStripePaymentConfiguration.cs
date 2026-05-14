using DainnStripe.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DainnStripe.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for local payment records.
/// </summary>
public class DainnStripePaymentConfiguration : IEntityTypeConfiguration<DainnStripePayment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DainnStripePayment> builder)
    {
        builder.ToTable("DainnStripePayments");

        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.OwnerId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(payment => payment.StripeCheckoutSessionId)
            .HasMaxLength(128);

        builder.Property(payment => payment.StripePaymentIntentId)
            .HasMaxLength(128);

        builder.Property(payment => payment.StripeCustomerId)
            .HasMaxLength(128);

        builder.Property(payment => payment.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(payment => payment.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(payment => payment.CreatedAt)
            .IsRequired();

        builder.Property(payment => payment.UpdatedAt)
            .IsRequired();

        builder.HasIndex(payment => payment.OwnerId);
        builder.HasIndex(payment => payment.StripeCheckoutSessionId).IsUnique();
        builder.HasIndex(payment => payment.StripePaymentIntentId);
    }
}
