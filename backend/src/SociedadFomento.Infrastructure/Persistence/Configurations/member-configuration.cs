using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SociedadFomento.Domain.Constants;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Infrastructure.Persistence.Configurations;

internal sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Members", table =>
        {
            table.HasCheckConstraint("CK_Members_Dni_NotEmpty", "LEN([Dni]) > 0");
            table.HasCheckConstraint("CK_Members_FirstName_NotEmpty", "LEN([FirstName]) > 0");
            table.HasCheckConstraint("CK_Members_LastName_NotEmpty", "LEN([LastName]) > 0");
            table.HasCheckConstraint("CK_Members_Address_NotEmpty", "LEN([Address]) > 0");
            table.HasCheckConstraint("CK_Members_Phone_NotEmpty", "LEN([Phone]) > 0");
            table.HasCheckConstraint("CK_Members_Status", "[Status] IN (1, 2)");
        });
        builder.HasKey(member => member.Id);
        builder.Property(member => member.Dni).HasMaxLength(MemberFieldLengths.Dni).IsUnicode(false).IsRequired();
        builder.Property(member => member.FirstName).HasMaxLength(MemberFieldLengths.FirstName).IsRequired();
        builder.Property(member => member.LastName).HasMaxLength(MemberFieldLengths.LastName).IsRequired();
        builder.Property(member => member.Address).HasMaxLength(MemberFieldLengths.Address).IsRequired();
        builder.Property(member => member.Phone).HasMaxLength(MemberFieldLengths.Phone).IsUnicode(false).IsRequired();
        builder.Property(member => member.Email).HasMaxLength(MemberFieldLengths.Email);
        builder.Property(member => member.Status).HasConversion<byte>().HasColumnType("tinyint");
        builder.Property(member => member.RowVersion).IsRowVersion();
        builder.HasIndex(member => member.Dni).IsUnique();
        builder.HasIndex(member => new { member.LastName, member.FirstName });
    }
}
