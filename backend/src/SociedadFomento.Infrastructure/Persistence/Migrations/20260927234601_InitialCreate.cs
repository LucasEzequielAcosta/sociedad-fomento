using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SociedadFomento.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeeRates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EffectiveMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeRates", x => x.Id);
                    table.CheckConstraint("CK_FeeRates_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_FeeRates_EffectiveMonth", "DAY([EffectiveMonth]) = 1");
                });

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dni = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Phone = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    RegistrationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.Id);
                    table.CheckConstraint("CK_Members_Address_NotEmpty", "LEN([Address]) > 0");
                    table.CheckConstraint("CK_Members_Dni_NotEmpty", "LEN([Dni]) > 0");
                    table.CheckConstraint("CK_Members_FirstName_NotEmpty", "LEN([FirstName]) > 0");
                    table.CheckConstraint("CK_Members_LastName_NotEmpty", "LEN([LastName]) > 0");
                    table.CheckConstraint("CK_Members_Phone_NotEmpty", "LEN([Phone]) > 0");
                    table.CheckConstraint("CK_Members_Status", "[Status] IN (1, 2)");
                });

            migrationBuilder.CreateTable(
                name: "MembershipPeriods",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<long>(type: "bigint", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipPeriods", x => x.Id);
                    table.CheckConstraint("CK_MembershipPeriods_Dates", "[EndDate] IS NULL OR [EndDate] >= [StartDate]");
                    table.ForeignKey(
                        name: "FK_MembershipPeriods_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<long>(type: "bigint", nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Method = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.CheckConstraint("CK_Payments_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_Payments_Cancellation", "([Status] = 1 AND [CancelledAtUtc] IS NULL) OR ([Status] = 2 AND [CancelledAtUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_Payments_Method", "[Method] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_Payments_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FeeObligations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<long>(type: "bigint", nullable: false),
                    MembershipPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    FeeRateId = table.Column<long>(type: "bigint", nullable: false),
                    Period = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeObligations", x => x.Id);
                    table.CheckConstraint("CK_FeeObligations_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_FeeObligations_Period", "DAY([Period]) = 1");
                    table.ForeignKey(
                        name: "FK_FeeObligations_FeeRates_FeeRateId",
                        column: x => x.FeeRateId,
                        principalTable: "FeeRates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeeObligations_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeeObligations_MembershipPeriods_MembershipPeriodId",
                        column: x => x.MembershipPeriodId,
                        principalTable: "MembershipPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Type = table.Column<byte>(type: "tinyint", nullable: false),
                    Source = table.Column<byte>(type: "tinyint", nullable: false),
                    Concept = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    PaymentId = table.Column<long>(type: "bigint", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingEntries", x => x.Id);
                    table.CheckConstraint("CK_AccountingEntries_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_AccountingEntries_Cancellation", "([Status] = 1 AND [CancelledAtUtc] IS NULL) OR ([Status] = 2 AND [CancelledAtUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_AccountingEntries_Source", "([Source] = 1 AND [Type] = 1 AND [PaymentId] IS NOT NULL) OR ([Source] = 2 AND [PaymentId] IS NULL)");
                    table.CheckConstraint("CK_AccountingEntries_Type", "[Type] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_AccountingEntries_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentId = table.Column<long>(type: "bigint", nullable: false),
                    FeeObligationId = table.Column<long>(type: "bigint", nullable: false),
                    ReleasedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_FeeObligations_FeeObligationId",
                        column: x => x.FeeObligationId,
                        principalTable: "FeeObligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingEntries_EntryDate_Type_Status",
                table: "AccountingEntries",
                columns: new[] { "EntryDate", "Type", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingEntries_PaymentId",
                table: "AccountingEntries",
                column: "PaymentId",
                unique: true,
                filter: "[PaymentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FeeObligations_FeeRateId",
                table: "FeeObligations",
                column: "FeeRateId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeObligations_MemberId_Period",
                table: "FeeObligations",
                columns: new[] { "MemberId", "Period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeeObligations_MembershipPeriodId",
                table: "FeeObligations",
                column: "MembershipPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeObligations_Period",
                table: "FeeObligations",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_FeeRates_EffectiveMonth",
                table: "FeeRates",
                column: "EffectiveMonth",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_Dni",
                table: "Members",
                column: "Dni",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_LastName_FirstName",
                table: "Members",
                columns: new[] { "LastName", "FirstName" });

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPeriods_MemberId",
                table: "MembershipPeriods",
                column: "MemberId",
                unique: true,
                filter: "[EndDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPeriods_MemberId_StartDate",
                table: "MembershipPeriods",
                columns: new[] { "MemberId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_FeeObligationId",
                table: "PaymentAllocations",
                column: "FeeObligationId",
                unique: true,
                filter: "[ReleasedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_PaymentId_FeeObligationId",
                table: "PaymentAllocations",
                columns: new[] { "PaymentId", "FeeObligationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MemberId_PaymentDate",
                table: "Payments",
                columns: new[] { "MemberId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentDate_Method",
                table: "Payments",
                columns: new[] { "PaymentDate", "Method" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingEntries");

            migrationBuilder.DropTable(
                name: "PaymentAllocations");

            migrationBuilder.DropTable(
                name: "FeeObligations");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "FeeRates");

            migrationBuilder.DropTable(
                name: "MembershipPeriods");

            migrationBuilder.DropTable(
                name: "Members");
        }
    }
}
