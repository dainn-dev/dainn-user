using DainnStripe.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DainnStripe.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for Stripe webhook event records.
/// </summary>
public class StripeWebhookEventRecordConfiguration : IEntityTypeConfiguration<StripeWebhookEventRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StripeWebhookEventRecord> builder)
    {
        builder.ToTable("StripeWebhookEvents");

        builder.HasKey(record => record.Id);

        builder.Property(record => record.StripeEventId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(record => record.EventType)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(record => record.ApiVersion)
            .HasMaxLength(64);

        builder.Property(record => record.StripeAccountId)
            .HasMaxLength(128);

        builder.Property(record => record.Payload)
            .IsRequired();

        builder.Property(record => record.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(record => record.ErrorMessage)
            .HasMaxLength(2048);

        builder.Property(record => record.CreatedAt)
            .IsRequired();

        builder.Property(record => record.UpdatedAt)
            .IsRequired();

        builder.HasIndex(record => record.StripeEventId).IsUnique();
        builder.HasIndex(record => record.EventType);
        builder.HasIndex(record => record.Status);
        builder.HasIndex(record => record.CreatedAt);
    }
}
