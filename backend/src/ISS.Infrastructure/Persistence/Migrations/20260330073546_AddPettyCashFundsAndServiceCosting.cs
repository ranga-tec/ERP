using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPettyCashFundsAndServiceCosting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SettlementPettyCashFundId",
                table: "ServiceExpenseClaims",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConvertedToEstimateAt",
                table: "ServiceExpenseClaimLine",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConvertedToServiceEstimateId",
                table: "ServiceExpenseClaimLine",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConvertedToServiceEstimateLineId",
                table: "ServiceExpenseClaimLine",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PettyCashFunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CustodianName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PettyCashFunds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PettyCashTransaction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PettyCashFundId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PettyCashTransaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PettyCashTransaction_PettyCashFunds_PettyCashFundId",
                        column: x => x.PettyCashFundId,
                        principalTable: "PettyCashFunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceExpenseClaims_SettlementPettyCashFundId",
                table: "ServiceExpenseClaims",
                column: "SettlementPettyCashFundId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceExpenseClaimLine_ConvertedToServiceEstimateId",
                table: "ServiceExpenseClaimLine",
                column: "ConvertedToServiceEstimateId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceExpenseClaimLine_ConvertedToServiceEstimateLineId",
                table: "ServiceExpenseClaimLine",
                column: "ConvertedToServiceEstimateLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashFunds_Code",
                table: "PettyCashFunds",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashTransaction_PettyCashFundId_OccurredAt",
                table: "PettyCashTransaction",
                columns: new[] { "PettyCashFundId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashTransaction_ReferenceType_ReferenceId",
                table: "PettyCashTransaction",
                columns: new[] { "ReferenceType", "ReferenceId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceExpenseClaims_PettyCashFunds_SettlementPettyCashFund~",
                table: "ServiceExpenseClaims",
                column: "SettlementPettyCashFundId",
                principalTable: "PettyCashFunds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceExpenseClaims_PettyCashFunds_SettlementPettyCashFund~",
                table: "ServiceExpenseClaims");

            migrationBuilder.DropTable(
                name: "PettyCashTransaction");

            migrationBuilder.DropTable(
                name: "PettyCashFunds");

            migrationBuilder.DropIndex(
                name: "IX_ServiceExpenseClaims_SettlementPettyCashFundId",
                table: "ServiceExpenseClaims");

            migrationBuilder.DropIndex(
                name: "IX_ServiceExpenseClaimLine_ConvertedToServiceEstimateId",
                table: "ServiceExpenseClaimLine");

            migrationBuilder.DropIndex(
                name: "IX_ServiceExpenseClaimLine_ConvertedToServiceEstimateLineId",
                table: "ServiceExpenseClaimLine");

            migrationBuilder.DropColumn(
                name: "SettlementPettyCashFundId",
                table: "ServiceExpenseClaims");

            migrationBuilder.DropColumn(
                name: "ConvertedToEstimateAt",
                table: "ServiceExpenseClaimLine");

            migrationBuilder.DropColumn(
                name: "ConvertedToServiceEstimateId",
                table: "ServiceExpenseClaimLine");

            migrationBuilder.DropColumn(
                name: "ConvertedToServiceEstimateLineId",
                table: "ServiceExpenseClaimLine");
        }
    }
}
