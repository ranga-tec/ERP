using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderLaborEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkOrderTimeEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicianUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TechnicianName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    WorkDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    WorkDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    HoursWorked = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CostRate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BillableToCustomer = table.Column<bool>(type: "boolean", nullable: false),
                    BillableHours = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BillingRate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxPercent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SalesInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    SalesInvoiceLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoicedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderTimeEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderTimeEntries_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderTimeEntries_SalesInvoiceId",
                table: "WorkOrderTimeEntries",
                column: "SalesInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderTimeEntries_SalesInvoiceLineId",
                table: "WorkOrderTimeEntries",
                column: "SalesInvoiceLineId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderTimeEntries_ServiceJobId",
                table: "WorkOrderTimeEntries",
                column: "ServiceJobId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderTimeEntries_TechnicianUserId",
                table: "WorkOrderTimeEntries",
                column: "TechnicianUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderTimeEntries_WorkOrderId_WorkDate",
                table: "WorkOrderTimeEntries",
                columns: new[] { "WorkOrderId", "WorkDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkOrderTimeEntries");
        }
    }
}
