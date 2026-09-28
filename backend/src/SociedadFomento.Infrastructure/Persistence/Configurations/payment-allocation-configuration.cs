using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Infrastructure.Persistence.Configurations;

internal sealed class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        builder.ToTable("PaymentAllocations");
        builder.HasKey(allocation => allocation.Id);
        builder.HasIndex(allocation => new { allocation.PaymentId, allocation.FeeObligationId }).IsUnique();
        builder.HasIndex(allocation => allocation.FeeObligationId).IsUnique().HasFilter("[ReleasedAtUtc] IS NULL");
        builder.HasOne(allocation => allocation.Payment).WithMany(payment => payment.PaymentAllocations)
            .HasForeignKey(allocation => new { allocation.PaymentId, allocation.MemberId })
            .HasPrincipalKey(payment => new { payment.Id, payment.MemberId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(allocation => allocation.FeeObligation).WithMany(obligation => obligation.PaymentAllocations)
            .HasForeignKey(allocation => new { allocation.FeeObligationId, allocation.MemberId })
            .HasPrincipalKey(obligation => new { obligation.Id, obligation.MemberId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
