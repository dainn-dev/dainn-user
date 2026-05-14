using DainnStripe.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DainnStripe.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for Stripe transfers.
/// </summary>
public class DainnStripeTransferConfiguration : IEntityTypeConfiguration<DainnStripeTransfer>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DainnStripeTransfer> builder)
    {
        builder.ToTable("DainnStripeTransfers");

        builder.HasKey(transfer => transfer.Id);

        builder.Property(transfer => transfer.OwnerId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(transfer => transfer.StripeTransferId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(transfer => transfer.StripeDestinationAccountId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(transfer => transfer.StripeSourceTransactionId).HasMaxLength(128);
        builder.Property(transfer => transfer.Currency).IsRequired().HasMaxLength(3);
        builder.Property(transfer => transfer.TransferGroup).HasMaxLength(128);
        builder.Property(transfer => transfer.CreatedAt).IsRequired();
        builder.Property(transfer => transfer.UpdatedAt).IsRequired();

        builder.HasOne(transfer => transfer.ConnectedAccount)
            .WithMany(account => account.Transfers)
            .HasForeignKey(transfer => transfer.ConnectedAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(transfer => transfer.OwnerId);
        builder.HasIndex(transfer => transfer.StripeTransferId).IsUnique();
        builder.HasIndex(transfer => transfer.StripeDestinationAccountId);
    }
}
