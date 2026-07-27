using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesInvoiceLineMaterialLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MaterialRequisitionLineId",
                table: "SalesInvoiceLine",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceLine_MaterialRequisitionLineId",
                table: "SalesInvoiceLine",
                column: "MaterialRequisitionLineId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesInvoiceLine_MaterialRequisitionLineId",
                table: "SalesInvoiceLine");

            migrationBuilder.DropColumn(
                name: "MaterialRequisitionLineId",
                table: "SalesInvoiceLine");
        }
    }
}
