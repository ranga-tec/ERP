using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddItemAccountAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExpenseAccountId",
                table: "Items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RevenueAccountId",
                table: "Items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_ExpenseAccountId",
                table: "Items",
                column: "ExpenseAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_RevenueAccountId",
                table: "Items",
                column: "RevenueAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_LedgerAccounts_ExpenseAccountId",
                table: "Items",
                column: "ExpenseAccountId",
                principalTable: "LedgerAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Items_LedgerAccounts_RevenueAccountId",
                table: "Items",
                column: "RevenueAccountId",
                principalTable: "LedgerAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_LedgerAccounts_ExpenseAccountId",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_LedgerAccounts_RevenueAccountId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_ExpenseAccountId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_RevenueAccountId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ExpenseAccountId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "RevenueAccountId",
                table: "Items");
        }
    }
}
