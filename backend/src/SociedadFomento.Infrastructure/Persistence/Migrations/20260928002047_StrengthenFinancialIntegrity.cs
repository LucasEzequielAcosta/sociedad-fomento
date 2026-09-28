using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SociedadFomento.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StrengthenFinancialIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FeeObligations_FeeRates_FeeRateId",
                table: "FeeObligations");

            migrationBuilder.DropForeignKey(
                name: "FK_FeeObligations_MembershipPeriods_MembershipPeriodId",
                table: "FeeObligations");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentAllocations_FeeObligations_FeeObligationId",
                table: "PaymentAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentAllocations_Payments_PaymentId",
                table: "PaymentAllocations");

            migrationBuilder.AddColumn<long>(
                name: "MemberId",
                table: "PaymentAllocations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql("""
                UPDATE allocation
                SET MemberId = payment.MemberId
                FROM PaymentAllocations AS allocation
                INNER JOIN Payments AS payment ON payment.Id = allocation.PaymentId;
                """);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Payments_Id_MemberId",
                table: "Payments",
                columns: new[] { "Id", "MemberId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_MembershipPeriods_Id_MemberId",
                table: "MembershipPeriods",
                columns: new[] { "Id", "MemberId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_FeeRates_Id_Amount",
                table: "FeeRates",
                columns: new[] { "Id", "Amount" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_FeeObligations_Id_MemberId",
                table: "FeeObligations",
                columns: new[] { "Id", "MemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_FeeObligationId_MemberId",
                table: "PaymentAllocations",
                columns: new[] { "FeeObligationId", "MemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_PaymentId_MemberId",
                table: "PaymentAllocations",
                columns: new[] { "PaymentId", "MemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_FeeObligations_FeeRateId_Amount",
                table: "FeeObligations",
                columns: new[] { "FeeRateId", "Amount" });

            migrationBuilder.CreateIndex(
                name: "IX_FeeObligations_MembershipPeriodId_MemberId",
                table: "FeeObligations",
                columns: new[] { "MembershipPeriodId", "MemberId" });

            migrationBuilder.AddForeignKey(
                name: "FK_FeeObligations_FeeRates_FeeRateId_Amount",
                table: "FeeObligations",
                columns: new[] { "FeeRateId", "Amount" },
                principalTable: "FeeRates",
                principalColumns: new[] { "Id", "Amount" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FeeObligations_MembershipPeriods_MembershipPeriodId_MemberId",
                table: "FeeObligations",
                columns: new[] { "MembershipPeriodId", "MemberId" },
                principalTable: "MembershipPeriods",
                principalColumns: new[] { "Id", "MemberId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentAllocations_FeeObligations_FeeObligationId_MemberId",
                table: "PaymentAllocations",
                columns: new[] { "FeeObligationId", "MemberId" },
                principalTable: "FeeObligations",
                principalColumns: new[] { "Id", "MemberId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentAllocations_Payments_PaymentId_MemberId",
                table: "PaymentAllocations",
                columns: new[] { "PaymentId", "MemberId" },
                principalTable: "Payments",
                principalColumns: new[] { "Id", "MemberId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FeeObligations_FeeRates_FeeRateId_Amount",
                table: "FeeObligations");

            migrationBuilder.DropForeignKey(
                name: "FK_FeeObligations_MembershipPeriods_MembershipPeriodId_MemberId",
                table: "FeeObligations");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentAllocations_FeeObligations_FeeObligationId_MemberId",
                table: "PaymentAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentAllocations_Payments_PaymentId_MemberId",
                table: "PaymentAllocations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Payments_Id_MemberId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAllocations_FeeObligationId_MemberId",
                table: "PaymentAllocations");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAllocations_PaymentId_MemberId",
                table: "PaymentAllocations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_MembershipPeriods_Id_MemberId",
                table: "MembershipPeriods");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_FeeRates_Id_Amount",
                table: "FeeRates");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_FeeObligations_Id_MemberId",
                table: "FeeObligations");

            migrationBuilder.DropIndex(
                name: "IX_FeeObligations_FeeRateId_Amount",
                table: "FeeObligations");

            migrationBuilder.DropIndex(
                name: "IX_FeeObligations_MembershipPeriodId_MemberId",
                table: "FeeObligations");

            migrationBuilder.DropColumn(
                name: "MemberId",
                table: "PaymentAllocations");

            migrationBuilder.AddForeignKey(
                name: "FK_FeeObligations_FeeRates_FeeRateId",
                table: "FeeObligations",
                column: "FeeRateId",
                principalTable: "FeeRates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FeeObligations_MembershipPeriods_MembershipPeriodId",
                table: "FeeObligations",
                column: "MembershipPeriodId",
                principalTable: "MembershipPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentAllocations_FeeObligations_FeeObligationId",
                table: "PaymentAllocations",
                column: "FeeObligationId",
                principalTable: "FeeObligations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentAllocations_Payments_PaymentId",
                table: "PaymentAllocations",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
