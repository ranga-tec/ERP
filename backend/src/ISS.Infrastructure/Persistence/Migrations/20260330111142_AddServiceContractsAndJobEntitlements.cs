using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceContractsAndJobEntitlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerBillingTreatment",
                table: "ServiceJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntitlementCoverage",
                table: "ServiceJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EntitlementEvaluatedAt",
                table: "ServiceJobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EntitlementSource",
                table: "ServiceJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EntitlementSummary",
                table: "ServiceJobs",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceContractId",
                table: "ServiceJobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarrantyCoverage",
                table: "EquipmentUnits",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE "EquipmentUnits"
                SET "WarrantyCoverage" = 4
                WHERE "WarrantyUntil" IS NOT NULL;
                """);

            migrationBuilder.CreateTable(
                name: "ServiceContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractType = table.Column<int>(type: "integer", nullable: false),
                    Coverage = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceContracts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceContracts_EquipmentUnits_EquipmentUnitId",
                        column: x => x.EquipmentUnitId,
                        principalTable: "EquipmentUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobs_ServiceContractId",
                table: "ServiceJobs",
                column: "ServiceContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceContracts_CustomerId",
                table: "ServiceContracts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceContracts_EquipmentUnitId",
                table: "ServiceContracts",
                column: "EquipmentUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceContracts_IsActive_StartDate_EndDate",
                table: "ServiceContracts",
                columns: new[] { "IsActive", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceContracts_Number",
                table: "ServiceContracts",
                column: "Number",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceJobs_ServiceContracts_ServiceContractId",
                table: "ServiceJobs",
                column: "ServiceContractId",
                principalTable: "ServiceContracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceJobs_ServiceContracts_ServiceContractId",
                table: "ServiceJobs");

            migrationBuilder.DropTable(
                name: "ServiceContracts");

            migrationBuilder.DropIndex(
                name: "IX_ServiceJobs_ServiceContractId",
                table: "ServiceJobs");

            migrationBuilder.DropColumn(
                name: "CustomerBillingTreatment",
                table: "ServiceJobs");

            migrationBuilder.DropColumn(
                name: "EntitlementCoverage",
                table: "ServiceJobs");

            migrationBuilder.DropColumn(
                name: "EntitlementEvaluatedAt",
                table: "ServiceJobs");

            migrationBuilder.DropColumn(
                name: "EntitlementSource",
                table: "ServiceJobs");

            migrationBuilder.DropColumn(
                name: "EntitlementSummary",
                table: "ServiceJobs");

            migrationBuilder.DropColumn(
                name: "ServiceContractId",
                table: "ServiceJobs");

            migrationBuilder.DropColumn(
                name: "WarrantyCoverage",
                table: "EquipmentUnits");
        }
    }
}
