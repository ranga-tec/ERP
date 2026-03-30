using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceEstimateCustomerApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerApprovalStatus",
                table: "ServiceEstimates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CustomerDecisionAt",
                table: "ServiceEstimates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SentToCustomerAt",
                table: "ServiceEstimates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "ServiceEstimates"
                SET "CustomerApprovalStatus" = CASE
                    WHEN "Status" = 1 THEN 2
                    WHEN "Status" = 2 THEN 3
                    ELSE 0
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerApprovalStatus",
                table: "ServiceEstimates");

            migrationBuilder.DropColumn(
                name: "CustomerDecisionAt",
                table: "ServiceEstimates");

            migrationBuilder.DropColumn(
                name: "SentToCustomerAt",
                table: "ServiceEstimates");
        }
    }
}
