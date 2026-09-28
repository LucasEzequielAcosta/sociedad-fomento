using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SociedadFomento.Domain.Constants;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Infrastructure.Persistence.Configurations;

internal sealed class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("AdminUsers", table =>
        {
            table.HasCheckConstraint("CK_AdminUsers_Email", "LEN([Email]) > 0");
            table.HasCheckConstraint("CK_AdminUsers_NormalizedEmail", "LEN([NormalizedEmail]) > 0");
            table.HasCheckConstraint("CK_AdminUsers_PasswordHash", "LEN([PasswordHash]) > 0");
            table.HasCheckConstraint("CK_AdminUsers_Role", "[Role] = 1");
        });
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Email).HasMaxLength(AdminUserFieldLengths.Email).IsRequired();
        builder.Property(user => user.NormalizedEmail).HasMaxLength(AdminUserFieldLengths.NormalizedEmail).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(AdminUserFieldLengths.PasswordHash).IsRequired();
        builder.Property(user => user.Role).HasConversion<byte>().HasColumnType("tinyint");
        builder.Property(user => user.RowVersion).IsRowVersion();
        builder.HasIndex(user => user.NormalizedEmail).IsUnique();
    }
}
