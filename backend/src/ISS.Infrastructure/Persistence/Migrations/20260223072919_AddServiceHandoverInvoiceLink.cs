using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceHandoverInvoiceLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConvertedToInvoiceAt",
                table: "ServiceHandovers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SalesInvoiceId",
                table: "ServiceHandovers",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConvertedToInvoiceAt",
                table: "ServiceHandovers");

            migrationBuilder.DropColumn(
                name: "SalesInvoiceId",
                table: "ServiceHandovers");
        }
    }
}
