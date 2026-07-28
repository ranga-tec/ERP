using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPettyCashIssueTrackingAndOverheadVouchers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "ServiceJobId",
                table: "ServiceExpenseClaims",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "PettyCashRequestLineId",
                table: "ServiceExpenseClaims",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssueBillNumber",
                table: "PettyCashIous",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PettyCashRequestLineId",
                table: "PettyCashIous",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SettlementApprovedAt",
                table: "PettyCashIous",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SettlementApprovedByUserId",
                table: "PettyCashIous",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceExpenseClaims_PettyCashRequestLineId",
                table: "ServiceExpenseClaims",
                column: "PettyCashRequestLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashIous_PettyCashRequestLineId",
                table: "PettyCashIous",
                column: "PettyCashRequestLineId");

            migrationBuilder.AddForeignKey(
                name: "FK_PettyCashIous_PettyCashRequestLine_PettyCashRequestLineId",
                table: "PettyCashIous",
                column: "PettyCashRequestLineId",
                principalTable: "PettyCashRequestLine",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceExpenseClaims_PettyCashRequestLine_PettyCashRequestL~",
                table: "ServiceExpenseClaims",
                column: "PettyCashRequestLineId",
                principalTable: "PettyCashRequestLine",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PettyCashIous_PettyCashRequestLine_PettyCashRequestLineId",
                table: "PettyCashIous");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceExpenseClaims_PettyCashRequestLine_PettyCashRequestL~",
                table: "ServiceExpenseClaims");

            migrationBuilder.DropIndex(
                name: "IX_ServiceExpenseClaims_PettyCashRequestLineId",
                table: "ServiceExpenseClaims");

            migrationBuilder.DropIndex(
                name: "IX_PettyCashIous_PettyCashRequestLineId",
                table: "PettyCashIous");

            migrationBuilder.DropColumn(
                name: "PettyCashRequestLineId",
                table: "ServiceExpenseClaims");

            migrationBuilder.DropColumn(
                name: "IssueBillNumber",
                table: "PettyCashIous");

            migrationBuilder.DropColumn(
                name: "PettyCashRequestLineId",
                table: "PettyCashIous");

            migrationBuilder.DropColumn(
                name: "SettlementApprovedAt",
                table: "PettyCashIous");

            migrationBuilder.DropColumn(
                name: "SettlementApprovedByUserId",
                table: "PettyCashIous");

            migrationBuilder.AlterColumn<Guid>(
                name: "ServiceJobId",
                table: "ServiceExpenseClaims",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
