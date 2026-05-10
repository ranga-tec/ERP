using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierAuthorizationServiceSchedulingAndPettyCashIous : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAuthorized",
                table: "Suppliers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpectedCompletionAt",
                table: "ServiceJobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SiteLocation",
                table: "ServiceJobs",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "MaterialRequisitions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextRepairDueAt",
                table: "EquipmentUnits",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextServiceDueAt",
                table: "EquipmentUnits",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceIntervalDays",
                table: "EquipmentUnits",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnforceAuthorizedSuppliersOnly",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PettyCashIous",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ServiceJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpectedSettlementAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    PettyCashFundId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReleasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReleaseReference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SettledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SettledAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    SettlementReference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PettyCashIous", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PettyCashIous_PettyCashFunds_PettyCashFundId",
                        column: x => x.PettyCashFundId,
                        principalTable: "PettyCashFunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashIous_Number",
                table: "PettyCashIous",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashIous_PettyCashFundId",
                table: "PettyCashIous",
                column: "PettyCashFundId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashIous_RequestedByUserId",
                table: "PettyCashIous",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashIous_ServiceJobId",
                table: "PettyCashIous",
                column: "ServiceJobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PettyCashIous");

            migrationBuilder.DropColumn(
                name: "IsAuthorized",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "ExpectedCompletionAt",
                table: "ServiceJobs");

            migrationBuilder.DropColumn(
                name: "SiteLocation",
                table: "ServiceJobs");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "MaterialRequisitions");

            migrationBuilder.DropColumn(
                name: "NextRepairDueAt",
                table: "EquipmentUnits");

            migrationBuilder.DropColumn(
                name: "NextServiceDueAt",
                table: "EquipmentUnits");

            migrationBuilder.DropColumn(
                name: "ServiceIntervalDays",
                table: "EquipmentUnits");

            migrationBuilder.DropColumn(
                name: "EnforceAuthorizedSuppliersOnly",
                table: "Companies");
        }
    }
}
