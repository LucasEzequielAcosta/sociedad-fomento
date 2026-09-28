using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Infrastructure.Persistence.Configurations;

internal sealed class MembershipPeriodConfiguration : IEntityTypeConfiguration<MembershipPeriod>
{
    public void Configure(EntityTypeBuilder<MembershipPeriod> builder)
    {
        builder.ToTable("MembershipPeriods", table =>
            table.HasCheckConstraint("CK_MembershipPeriods_Dates", "[EndDate] IS NULL OR [EndDate] >= [StartDate]"));
        builder.HasKey(period => period.Id);
        builder.HasAlternateKey(period => new { period.Id, period.MemberId });
        builder.Property(period => period.RowVersion).IsRowVersion();
        builder.HasIndex(period => new { period.MemberId, period.StartDate });
        builder.HasIndex(period => period.MemberId).IsUnique().HasFilter("[EndDate] IS NULL");
        builder.HasOne(period => period.Member)
            .WithMany(member => member.MembershipPeriods)
            .HasForeignKey(period => period.MemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
