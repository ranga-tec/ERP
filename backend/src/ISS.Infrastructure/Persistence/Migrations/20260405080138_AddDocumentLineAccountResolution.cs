using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentLineAccountResolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExpenseAccountId",
                table: "ServiceExpenseClaimLine",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RevenueAccountId",
                table: "SalesInvoiceLine",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExpenseAccountId",
                table: "DirectPurchaseLine",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceExpenseClaimLine_ExpenseAccountId",
                table: "ServiceExpenseClaimLine",
                column: "ExpenseAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceLine_RevenueAccountId",
                table: "SalesInvoiceLine",
                column: "RevenueAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectPurchaseLine_ExpenseAccountId",
                table: "DirectPurchaseLine",
                column: "ExpenseAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_DirectPurchaseLine_LedgerAccounts_ExpenseAccountId",
                table: "DirectPurchaseLine",
                column: "ExpenseAccountId",
                principalTable: "LedgerAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoiceLine_LedgerAccounts_RevenueAccountId",
                table: "SalesInvoiceLine",
                column: "RevenueAccountId",
                principalTable: "LedgerAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceExpenseClaimLine_LedgerAccounts_ExpenseAccountId",
                table: "ServiceExpenseClaimLine",
                column: "ExpenseAccountId",
                principalTable: "LedgerAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DirectPurchaseLine_LedgerAccounts_ExpenseAccountId",
                table: "DirectPurchaseLine");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoiceLine_LedgerAccounts_RevenueAccountId",
                table: "SalesInvoiceLine");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceExpenseClaimLine_LedgerAccounts_ExpenseAccountId",
                table: "ServiceExpenseClaimLine");

            migrationBuilder.DropIndex(
                name: "IX_ServiceExpenseClaimLine_ExpenseAccountId",
                table: "ServiceExpenseClaimLine");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoiceLine_RevenueAccountId",
                table: "SalesInvoiceLine");

            migrationBuilder.DropIndex(
                name: "IX_DirectPurchaseLine_ExpenseAccountId",
                table: "DirectPurchaseLine");

            migrationBuilder.DropColumn(
                name: "ExpenseAccountId",
                table: "ServiceExpenseClaimLine");

            migrationBuilder.DropColumn(
                name: "RevenueAccountId",
                table: "SalesInvoiceLine");

            migrationBuilder.DropColumn(
                name: "ExpenseAccountId",
                table: "DirectPurchaseLine");
        }
    }
}
