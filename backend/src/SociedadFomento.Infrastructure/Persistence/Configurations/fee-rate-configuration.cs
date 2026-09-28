using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Infrastructure.Persistence.Configurations;

internal sealed class FeeRateConfiguration : IEntityTypeConfiguration<FeeRate>
{
    public void Configure(EntityTypeBuilder<FeeRate> builder)
    {
        builder.ToTable("FeeRates", table =>
        {
            table.HasCheckConstraint("CK_FeeRates_EffectiveMonth", "DAY([EffectiveMonth]) = 1");
            table.HasCheckConstraint("CK_FeeRates_Amount", "[Amount] > 0");
        });
        builder.HasKey(rate => rate.Id);
        builder.HasAlternateKey(rate => new { rate.Id, rate.Amount });
        builder.Property(rate => rate.Amount).HasPrecision(18, 2);
        builder.Property(rate => rate.RowVersion).IsRowVersion();
        builder.HasIndex(rate => rate.EffectiveMonth).IsUnique();
    }
}
