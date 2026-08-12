using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesInvoiceLineCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "SalesInvoiceLine",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE "SalesInvoiceLine" AS line
                SET "Category" = 1
                WHERE EXISTS (
                    SELECT 1 FROM "WorkOrderTimeEntries" AS labour
                    WHERE labour."SalesInvoiceLineId" = line."Id");

                UPDATE "SalesInvoiceLine" AS line
                SET "Category" = 2
                WHERE EXISTS (
                    SELECT 1 FROM "ServiceExpenseClaimLine" AS expense
                    WHERE expense."SalesInvoiceLineId" = line."Id");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "SalesInvoiceLine");
        }
    }
}
