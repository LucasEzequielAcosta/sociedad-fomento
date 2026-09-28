using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SociedadFomento.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Role = table.Column<byte>(type: "tinyint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.Id);
                    table.CheckConstraint("CK_AdminUsers_Email", "LEN([Email]) > 0");
                    table.CheckConstraint("CK_AdminUsers_NormalizedEmail", "LEN([NormalizedEmail]) > 0");
                    table.CheckConstraint("CK_AdminUsers_PasswordHash", "LEN([PasswordHash]) > 0");
                    table.CheckConstraint("CK_AdminUsers_Role", "[Role] = 1");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_NormalizedEmail",
                table: "AdminUsers",
                column: "NormalizedEmail",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminUsers");
        }
    }
}
