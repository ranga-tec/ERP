using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceJobMaterialDispositionAndInvoiceDecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FinalInvoiceNotRequired",
                table: "ServiceJobs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FinalInvoiceNotRequiredReason",
                table: "ServiceJobs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceJobMaterialDispositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialRequisitionLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Condition = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ChargeTo = table.Column<int>(type: "integer", nullable: false),
                    SupplierReturnId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResponsiblePerson = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceJobMaterialDispositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceJobMaterialDispositions_MaterialRequisitionLine_Mate~",
                        column: x => x.MaterialRequisitionLineId,
                        principalTable: "MaterialRequisitionLine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceJobMaterialDispositions_MaterialRequisitions_Materia~",
                        column: x => x.MaterialRequisitionId,
                        principalTable: "MaterialRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceJobMaterialDispositions_ServiceJobs_ServiceJobId",
                        column: x => x.ServiceJobId,
                        principalTable: "ServiceJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceJobMaterialDispositions_SupplierReturns_SupplierRetu~",
                        column: x => x.SupplierReturnId,
                        principalTable: "SupplierReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ServiceJobMaterialDispositionSerial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceJobMaterialDispositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceJobMaterialDispositionSerial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceJobMaterialDispositionSerial_ServiceJobMaterialDispo~",
                        column: x => x.ServiceJobMaterialDispositionId,
                        principalTable: "ServiceJobMaterialDispositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobMaterialDispositions_MaterialRequisitionId",
                table: "ServiceJobMaterialDispositions",
                column: "MaterialRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobMaterialDispositions_MaterialRequisitionLineId",
                table: "ServiceJobMaterialDispositions",
                column: "MaterialRequisitionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobMaterialDispositions_ServiceJobId",
                table: "ServiceJobMaterialDispositions",
                column: "ServiceJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobMaterialDispositions_SupplierReturnId",
                table: "ServiceJobMaterialDispositions",
                column: "SupplierReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobMaterialDispositionSerial_SerialNumber",
                table: "ServiceJobMaterialDispositionSerial",
                column: "SerialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobMaterialDispositionSerial_ServiceJobMaterialDispo~",
                table: "ServiceJobMaterialDispositionSerial",
                column: "ServiceJobMaterialDispositionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceJobMaterialDispositionSerial");

            migrationBuilder.DropTable(
                name: "ServiceJobMaterialDispositions");

            migrationBuilder.DropColumn(
                name: "FinalInvoiceNotRequired",
                table: "ServiceJobs");

            migrationBuilder.DropColumn(
                name: "FinalInvoiceNotRequiredReason",
                table: "ServiceJobs");
        }
    }
}
