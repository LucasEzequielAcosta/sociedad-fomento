using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Infrastructure.Persistence.Configurations;

internal sealed class FeeObligationConfiguration : IEntityTypeConfiguration<FeeObligation>
{
    public void Configure(EntityTypeBuilder<FeeObligation> builder)
    {
        builder.ToTable("FeeObligations", table =>
        {
            table.HasCheckConstraint("CK_FeeObligations_Period", "DAY([Period]) = 1");
            table.HasCheckConstraint("CK_FeeObligations_Amount", "[Amount] > 0");
        });
        builder.HasKey(obligation => obligation.Id);
        builder.HasAlternateKey(obligation => new { obligation.Id, obligation.MemberId });
        builder.Property(obligation => obligation.Amount).HasPrecision(18, 2);
        builder.Property(obligation => obligation.RowVersion).IsRowVersion();
        builder.HasIndex(obligation => new { obligation.MemberId, obligation.Period }).IsUnique();
        builder.HasIndex(obligation => obligation.Period);
        builder.HasIndex(obligation => obligation.FeeRateId);
        builder.HasIndex(obligation => obligation.MembershipPeriodId);
        builder.HasOne(obligation => obligation.Member).WithMany(member => member.FeeObligations)
            .HasForeignKey(obligation => obligation.MemberId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(obligation => obligation.MembershipPeriod).WithMany(period => period.FeeObligations)
            .HasForeignKey(obligation => new { obligation.MembershipPeriodId, obligation.MemberId })
            .HasPrincipalKey(period => new { period.Id, period.MemberId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(obligation => obligation.FeeRate).WithMany(rate => rate.FeeObligations)
            .HasForeignKey(obligation => new { obligation.FeeRateId, obligation.Amount })
            .HasPrincipalKey(rate => new { rate.Id, rate.Amount })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
