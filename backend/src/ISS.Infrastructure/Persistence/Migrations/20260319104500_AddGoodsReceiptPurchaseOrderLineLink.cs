using ISS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(IssDbContext))]
[Migration("20260319104500_AddGoodsReceiptPurchaseOrderLineLink")]
public partial class AddGoodsReceiptPurchaseOrderLineLink : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "PurchaseOrderLineId",
            table: "GoodsReceiptLine",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_GoodsReceiptLine_PurchaseOrderLineId",
            table: "GoodsReceiptLine",
            column: "PurchaseOrderLineId");

        migrationBuilder.AddForeignKey(
            name: "FK_GoodsReceiptLine_PurchaseOrderLine_PurchaseOrderLineId",
            table: "GoodsReceiptLine",
            column: "PurchaseOrderLineId",
            principalTable: "PurchaseOrderLine",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_GoodsReceiptLine_PurchaseOrderLine_PurchaseOrderLineId",
            table: "GoodsReceiptLine");

        migrationBuilder.DropIndex(
            name: "IX_GoodsReceiptLine_PurchaseOrderLineId",
            table: "GoodsReceiptLine");

        migrationBuilder.DropColumn(
            name: "PurchaseOrderLineId",
            table: "GoodsReceiptLine");
    }
}
