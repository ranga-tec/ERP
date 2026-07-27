using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceDiscountAndServiceChargeBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InvoicedAt",
                table: "ServiceExpenseClaimLine",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SalesInvoiceId",
                table: "ServiceExpenseClaimLine",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SalesInvoiceLineId",
                table: "ServiceExpenseClaimLine",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "SalesInvoices",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "SalesInvoices",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "SalesInvoiceLine",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceExpenseClaimLine_SalesInvoiceLineId",
                table: "ServiceExpenseClaimLine",
                column: "SalesInvoiceLineId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceExpenseClaimLine_SalesInvoiceLineId",
                table: "ServiceExpenseClaimLine");

            migrationBuilder.DropColumn(
                name: "InvoicedAt",
                table: "ServiceExpenseClaimLine");

            migrationBuilder.DropColumn(
                name: "SalesInvoiceId",
                table: "ServiceExpenseClaimLine");

            migrationBuilder.DropColumn(
                name: "SalesInvoiceLineId",
                table: "ServiceExpenseClaimLine");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "SalesInvoiceLine");
        }
    }
}
