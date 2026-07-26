using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectDispatchMaterialRequisitionLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MaterialRequisitionLineId",
                table: "DirectDispatchLine",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialRequisitionId",
                table: "DirectDispatches",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DirectDispatchLine_MaterialRequisitionLineId",
                table: "DirectDispatchLine",
                column: "MaterialRequisitionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectDispatches_MaterialRequisitionId",
                table: "DirectDispatches",
                column: "MaterialRequisitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DirectDispatchLine_MaterialRequisitionLineId",
                table: "DirectDispatchLine");

            migrationBuilder.DropIndex(
                name: "IX_DirectDispatches_MaterialRequisitionId",
                table: "DirectDispatches");

            migrationBuilder.DropColumn(
                name: "MaterialRequisitionLineId",
                table: "DirectDispatchLine");

            migrationBuilder.DropColumn(
                name: "MaterialRequisitionId",
                table: "DirectDispatches");
        }
    }
}
