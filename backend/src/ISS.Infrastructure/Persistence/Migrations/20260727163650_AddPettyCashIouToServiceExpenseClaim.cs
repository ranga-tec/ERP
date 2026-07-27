using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPettyCashIouToServiceExpenseClaim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PettyCashIouId",
                table: "ServiceExpenseClaims",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceExpenseClaims_PettyCashIouId",
                table: "ServiceExpenseClaims",
                column: "PettyCashIouId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceExpenseClaims_PettyCashIous_PettyCashIouId",
                table: "ServiceExpenseClaims",
                column: "PettyCashIouId",
                principalTable: "PettyCashIous",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceExpenseClaims_PettyCashIous_PettyCashIouId",
                table: "ServiceExpenseClaims");

            migrationBuilder.DropIndex(
                name: "IX_ServiceExpenseClaims_PettyCashIouId",
                table: "ServiceExpenseClaims");

            migrationBuilder.DropColumn(
                name: "PettyCashIouId",
                table: "ServiceExpenseClaims");
        }
    }
}
