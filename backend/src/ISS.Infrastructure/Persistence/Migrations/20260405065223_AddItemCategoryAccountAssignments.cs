using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddItemCategoryAccountAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExpenseAccountId",
                table: "ItemCategories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RevenueAccountId",
                table: "ItemCategories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemCategories_ExpenseAccountId",
                table: "ItemCategories",
                column: "ExpenseAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemCategories_RevenueAccountId",
                table: "ItemCategories",
                column: "RevenueAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemCategories_LedgerAccounts_ExpenseAccountId",
                table: "ItemCategories",
                column: "ExpenseAccountId",
                principalTable: "LedgerAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemCategories_LedgerAccounts_RevenueAccountId",
                table: "ItemCategories",
                column: "RevenueAccountId",
                principalTable: "LedgerAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemCategories_LedgerAccounts_ExpenseAccountId",
                table: "ItemCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemCategories_LedgerAccounts_RevenueAccountId",
                table: "ItemCategories");

            migrationBuilder.DropIndex(
                name: "IX_ItemCategories_ExpenseAccountId",
                table: "ItemCategories");

            migrationBuilder.DropIndex(
                name: "IX_ItemCategories_RevenueAccountId",
                table: "ItemCategories");

            migrationBuilder.DropColumn(
                name: "ExpenseAccountId",
                table: "ItemCategories");

            migrationBuilder.DropColumn(
                name: "RevenueAccountId",
                table: "ItemCategories");
        }
    }
}
