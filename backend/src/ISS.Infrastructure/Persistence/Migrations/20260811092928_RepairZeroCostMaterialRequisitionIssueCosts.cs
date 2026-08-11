using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RepairZeroCostMaterialRequisitionIssueCosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                WITH replacement_costs AS (
                    SELECT target."Id",
                           (
                               SELECT ROUND(
                                   SUM(source."Quantity" * source."UnitCost")
                                   / NULLIF(SUM(source."Quantity"), 0),
                                   4)
                               FROM "InventoryMovements" AS source
                               WHERE source."WarehouseId" = target."WarehouseId"
                                 AND source."ItemId" = target."ItemId"
                                 AND source."OccurredAt" <= target."OccurredAt"
                                 AND source."Quantity" > 0
                                 AND source."UnitCost" > 0
                                 AND source."BatchNumber" IS NOT DISTINCT FROM target."BatchNumber"
                                 AND source."SerialNumber" IS NOT DISTINCT FROM target."SerialNumber"
                           ) AS "UnitCost"
                    FROM "InventoryMovements" AS target
                    WHERE target."ReferenceType" = 'MR'
                      AND target."Type" = 6
                      AND target."UnitCost" = 0
                )
                UPDATE "InventoryMovements" AS target
                SET "UnitCost" = replacement."UnitCost"
                FROM replacement_costs AS replacement
                WHERE target."Id" = replacement."Id"
                  AND replacement."UnitCost" > 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Historical issue costs cannot be safely restored to zero.
        }
    }
}
