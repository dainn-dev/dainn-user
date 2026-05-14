using DainnStripe.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DainnStripe.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for credit note records.
/// </summary>
public class DainnStripeCreditNoteConfiguration : IEntityTypeConfiguration<DainnStripeCreditNote>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DainnStripeCreditNote> builder)
    {
        builder.ToTable("DainnStripeCreditNotes");

        builder.HasKey(note => note.Id);

        builder.Property(note => note.StripeCreditNoteId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(note => note.StripeInvoiceId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(note => note.OwnerId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(note => note.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(note => note.Reason)
            .HasMaxLength(256);

        builder.Property(note => note.Memo)
            .HasMaxLength(1024);

        builder.Property(note => note.CreatedAt)
            .IsRequired();

        builder.Property(note => note.UpdatedAt)
            .IsRequired();

        builder.HasIndex(note => note.StripeCreditNoteId).IsUnique();
        builder.HasIndex(note => note.StripeInvoiceId);
        builder.HasIndex(note => note.OwnerId);
    }
}
