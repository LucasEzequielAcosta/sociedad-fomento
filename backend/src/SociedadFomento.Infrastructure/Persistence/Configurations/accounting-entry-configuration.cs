using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SociedadFomento.Domain.Constants;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Infrastructure.Persistence.Configurations;

internal sealed class AccountingEntryConfiguration : IEntityTypeConfiguration<AccountingEntry>
{
    public void Configure(EntityTypeBuilder<AccountingEntry> builder)
    {
        builder.ToTable("AccountingEntries", table =>
        {
            table.HasCheckConstraint("CK_AccountingEntries_Amount", "[Amount] > 0");
            table.HasCheckConstraint("CK_AccountingEntries_Cancellation", "([Status] = 1 AND [CancelledAtUtc] IS NULL) OR ([Status] = 2 AND [CancelledAtUtc] IS NOT NULL)");
            table.HasCheckConstraint("CK_AccountingEntries_Source", "([Source] = 1 AND [Type] = 1 AND [PaymentId] IS NOT NULL) OR ([Source] = 2 AND [PaymentId] IS NULL)");
            table.HasCheckConstraint("CK_AccountingEntries_Type", "[Type] IN (1, 2)");
        });
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Concept).HasMaxLength(AccountingEntryFieldLengths.Concept).IsRequired();
        builder.Property(entry => entry.Amount).HasPrecision(18, 2);
        builder.Property(entry => entry.Type).HasConversion<byte>().HasColumnType("tinyint");
        builder.Property(entry => entry.Source).HasConversion<byte>().HasColumnType("tinyint");
        builder.Property(entry => entry.Status).HasConversion<byte>().HasColumnType("tinyint");
        builder.Property(entry => entry.RowVersion).IsRowVersion();
        builder.HasIndex(entry => entry.PaymentId).IsUnique().HasFilter("[PaymentId] IS NOT NULL");
        builder.HasIndex(entry => new { entry.EntryDate, entry.Type, entry.Status });
        builder.HasOne(entry => entry.Payment).WithOne(payment => payment.AccountingEntry)
            .HasForeignKey<AccountingEntry>(entry => entry.PaymentId).OnDelete(DeleteBehavior.Restrict);
    }
}
