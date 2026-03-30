using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceJobKindEstimateRevisionsAndDirectPurchaseJobLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "ServiceJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "RevisedFromEstimateId",
                table: "ServiceEstimates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RevisionNumber",
                table: "ServiceEstimates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceJobId",
                table: "DirectPurchases",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceEstimates_RevisedFromEstimateId",
                table: "ServiceEstimates",
                column: "RevisedFromEstimateId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectPurchases_ServiceJobId",
                table: "DirectPurchases",
                column: "ServiceJobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceEstimates_RevisedFromEstimateId",
                table: "ServiceEstimates");

            migrationBuilder.DropIndex(
                name: "IX_DirectPurchases_ServiceJobId",
                table: "DirectPurchases");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "ServiceJobs");

            migrationBuilder.DropColumn(
                name: "RevisedFromEstimateId",
                table: "ServiceEstimates");

            migrationBuilder.DropColumn(
                name: "RevisionNumber",
                table: "ServiceEstimates");

            migrationBuilder.DropColumn(
                name: "ServiceJobId",
                table: "DirectPurchases");
        }
    }
}
