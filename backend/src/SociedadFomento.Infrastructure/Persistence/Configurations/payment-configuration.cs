using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Infrastructure.Persistence.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", table =>
        {
            table.HasCheckConstraint("CK_Payments_Amount", "[Amount] > 0");
            table.HasCheckConstraint("CK_Payments_Cancellation", "([Status] = 1 AND [CancelledAtUtc] IS NULL) OR ([Status] = 2 AND [CancelledAtUtc] IS NOT NULL)");
            table.HasCheckConstraint("CK_Payments_Method", "[Method] IN (1, 2, 3)");
        });
        builder.HasKey(payment => payment.Id);
        builder.HasAlternateKey(payment => new { payment.Id, payment.MemberId });
        builder.Property(payment => payment.Amount).HasPrecision(18, 2);
        builder.Property(payment => payment.Method).HasConversion<byte>().HasColumnType("tinyint");
        builder.Property(payment => payment.Status).HasConversion<byte>().HasColumnType("tinyint");
        builder.Property(payment => payment.RowVersion).IsRowVersion();
        builder.HasIndex(payment => new { payment.MemberId, payment.PaymentDate });
        builder.HasIndex(payment => new { payment.PaymentDate, payment.Method });
        builder.HasOne(payment => payment.Member).WithMany(member => member.Payments)
            .HasForeignKey(payment => payment.MemberId).OnDelete(DeleteBehavior.Restrict);
    }
}
