using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PettyCashV2FundReplenishment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CostCenterCode",
                table: "ServiceExpenseClaims",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FundLevelAmount",
                table: "PettyCashReturns",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsLegacyCategoryReturn",
                table: "PettyCashReturns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedAmount",
                table: "PettyCashRequests",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CashOnHandAtRequest",
                table: "PettyCashRequests",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FundedAmount",
                table: "PettyCashRequests",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsLegacyCategoryRequest",
                table: "PettyCashRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "OutstandingAdvancesAtRequest",
                table: "PettyCashRequests",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReconciledExpensesAtRequest",
                table: "PettyCashRequests",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RequestedAmount",
                table: "PettyCashRequests",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SettlementExceptionAmount",
                table: "PettyCashIous",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SettlementExceptionApprovedAt",
                table: "PettyCashIous",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SettlementExceptionApprovedByUserId",
                table: "PettyCashIous",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SettlementExceptionReason",
                table: "PettyCashIous",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AdvanceLimit",
                table: "PettyCashFunds",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AuthorizedFloat",
                table: "PettyCashFunds",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "BlockOverdueAdvances",
                table: "PettyCashFunds",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "PettyCashFunds",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequireReceipt",
                table: "PettyCashFunds",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TransactionLimit",
                table: "PettyCashFunds",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            // Every record created before this deployment used category lines. Preserve those rows
            // as read-only history while new V2 requests and returns use fund-level amounts.
            migrationBuilder.Sql("UPDATE \"PettyCashRequests\" SET \"IsLegacyCategoryRequest\" = TRUE;");
            migrationBuilder.Sql("UPDATE \"PettyCashReturns\" SET \"IsLegacyCategoryReturn\" = TRUE;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostCenterCode",
                table: "ServiceExpenseClaims");

            migrationBuilder.DropColumn(
                name: "FundLevelAmount",
                table: "PettyCashReturns");

            migrationBuilder.DropColumn(
                name: "IsLegacyCategoryReturn",
                table: "PettyCashReturns");

            migrationBuilder.DropColumn(
                name: "ApprovedAmount",
                table: "PettyCashRequests");

            migrationBuilder.DropColumn(
                name: "CashOnHandAtRequest",
                table: "PettyCashRequests");

            migrationBuilder.DropColumn(
                name: "FundedAmount",
                table: "PettyCashRequests");

            migrationBuilder.DropColumn(
                name: "IsLegacyCategoryRequest",
                table: "PettyCashRequests");

            migrationBuilder.DropColumn(
                name: "OutstandingAdvancesAtRequest",
                table: "PettyCashRequests");

            migrationBuilder.DropColumn(
                name: "ReconciledExpensesAtRequest",
                table: "PettyCashRequests");

            migrationBuilder.DropColumn(
                name: "RequestedAmount",
                table: "PettyCashRequests");

            migrationBuilder.DropColumn(
                name: "SettlementExceptionAmount",
                table: "PettyCashIous");

            migrationBuilder.DropColumn(
                name: "SettlementExceptionApprovedAt",
                table: "PettyCashIous");

            migrationBuilder.DropColumn(
                name: "SettlementExceptionApprovedByUserId",
                table: "PettyCashIous");

            migrationBuilder.DropColumn(
                name: "SettlementExceptionReason",
                table: "PettyCashIous");

            migrationBuilder.DropColumn(
                name: "AdvanceLimit",
                table: "PettyCashFunds");

            migrationBuilder.DropColumn(
                name: "AuthorizedFloat",
                table: "PettyCashFunds");

            migrationBuilder.DropColumn(
                name: "BlockOverdueAdvances",
                table: "PettyCashFunds");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "PettyCashFunds");

            migrationBuilder.DropColumn(
                name: "RequireReceipt",
                table: "PettyCashFunds");

            migrationBuilder.DropColumn(
                name: "TransactionLimit",
                table: "PettyCashFunds");
        }
    }
}
