using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompletePettyCashPrdSuggestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PettyCashSpendingLimit",
                table: "ServiceJobs",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "MissingReceipt",
                table: "ServiceExpenseClaimLine",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MissingReceiptApprovedAt",
                table: "ServiceExpenseClaimLine",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MissingReceiptApprovedByUserId",
                table: "ServiceExpenseClaimLine",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MissingReceiptReason",
                table: "ServiceExpenseClaimLine",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptReference",
                table: "ServiceExpenseClaimLine",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SettlementExceptionExpenseClaimId",
                table: "PettyCashIous",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CashCountFrequency",
                table: "PettyCashFunds",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastCashCountAt",
                table: "PettyCashFunds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextCashCountDueAt",
                table: "PettyCashFunds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SettlementShortageCostCenterCode",
                table: "PettyCashFunds",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SettlementShortageExpenseAccountId",
                table: "PettyCashFunds",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PettyCashCashCounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PettyCashFundId = table.Column<Guid>(type: "uuid", nullable: false),
                    CountedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CountedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CountedByName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PhysicalCash = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    OutstandingAdvances = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    SupportedExpenseVouchers = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    AuthorizedFloatSnapshot = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PettyCashCashCounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PettyCashCashCounts_PettyCashFunds_PettyCashFundId",
                        column: x => x.PettyCashFundId,
                        principalTable: "PettyCashFunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                UPDATE "ServiceExpenseClaimLine" AS line
                SET "ReceiptReference" = claim."ReceiptReference"
                FROM "ServiceExpenseClaims" AS claim
                WHERE line."ServiceExpenseClaimId" = claim."Id"
                  AND claim."FundingSource" = 2
                  AND claim."ReceiptReference" IS NOT NULL
                  AND BTRIM(claim."ReceiptReference") <> '';

                UPDATE "PettyCashFunds"
                SET "CashCountFrequency" = 2,
                    "NextCashCountDueAt" = CURRENT_TIMESTAMP + INTERVAL '7 days'
                WHERE "IsActive" = TRUE;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashFunds_SettlementShortageExpenseAccountId",
                table: "PettyCashFunds",
                column: "SettlementShortageExpenseAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashCashCounts_Number",
                table: "PettyCashCashCounts",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashCashCounts_PettyCashFundId_CountedAt",
                table: "PettyCashCashCounts",
                columns: new[] { "PettyCashFundId", "CountedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashCashCounts_Status",
                table: "PettyCashCashCounts",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_PettyCashFunds_LedgerAccounts_SettlementShortageExpenseAcco~",
                table: "PettyCashFunds",
                column: "SettlementShortageExpenseAccountId",
                principalTable: "LedgerAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PettyCashFunds_LedgerAccounts_SettlementShortageExpenseAcco~",
                table: "PettyCashFunds");

            migrationBuilder.DropTable(name: "PettyCashCashCounts");

            migrationBuilder.DropIndex(
                name: "IX_PettyCashFunds_SettlementShortageExpenseAccountId",
                table: "PettyCashFunds");

            migrationBuilder.DropColumn(name: "PettyCashSpendingLimit", table: "ServiceJobs");
            migrationBuilder.DropColumn(name: "MissingReceipt", table: "ServiceExpenseClaimLine");
            migrationBuilder.DropColumn(name: "MissingReceiptApprovedAt", table: "ServiceExpenseClaimLine");
            migrationBuilder.DropColumn(name: "MissingReceiptApprovedByUserId", table: "ServiceExpenseClaimLine");
            migrationBuilder.DropColumn(name: "MissingReceiptReason", table: "ServiceExpenseClaimLine");
            migrationBuilder.DropColumn(name: "ReceiptReference", table: "ServiceExpenseClaimLine");
            migrationBuilder.DropColumn(name: "SettlementExceptionExpenseClaimId", table: "PettyCashIous");
            migrationBuilder.DropColumn(name: "CashCountFrequency", table: "PettyCashFunds");
            migrationBuilder.DropColumn(name: "LastCashCountAt", table: "PettyCashFunds");
            migrationBuilder.DropColumn(name: "NextCashCountDueAt", table: "PettyCashFunds");
            migrationBuilder.DropColumn(name: "SettlementShortageCostCenterCode", table: "PettyCashFunds");
            migrationBuilder.DropColumn(name: "SettlementShortageExpenseAccountId", table: "PettyCashFunds");
        }
    }
}
