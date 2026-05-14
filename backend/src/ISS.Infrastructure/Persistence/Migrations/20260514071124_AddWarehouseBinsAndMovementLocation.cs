using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseBinsAndMovementLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseBinId",
                table: "InventoryMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WarehouseBins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Zone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Rack = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Shelf = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseBins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseBins_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_WarehouseBinId",
                table: "InventoryMovements",
                column: "WarehouseBinId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_WarehouseId_WarehouseBinId_ItemId",
                table: "InventoryMovements",
                columns: new[] { "WarehouseId", "WarehouseBinId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseBins_WarehouseId",
                table: "WarehouseBins",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseBins_WarehouseId_Code",
                table: "WarehouseBins",
                columns: new[] { "WarehouseId", "Code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovements_WarehouseBins_WarehouseBinId",
                table: "InventoryMovements",
                column: "WarehouseBinId",
                principalTable: "WarehouseBins",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovements_WarehouseBins_WarehouseBinId",
                table: "InventoryMovements");

            migrationBuilder.DropTable(
                name: "WarehouseBins");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_WarehouseBinId",
                table: "InventoryMovements");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_WarehouseId_WarehouseBinId_ItemId",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "WarehouseBinId",
                table: "InventoryMovements");
        }
    }
}
