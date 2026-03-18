using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockAdjustmentCountModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CountedQuantity",
                table: "StockAdjustmentLine",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SystemQuantity",
                table: "StockAdjustmentLine",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountedQuantity",
                table: "StockAdjustmentLine");

            migrationBuilder.DropColumn(
                name: "SystemQuantity",
                table: "StockAdjustmentLine");
        }
    }
}
