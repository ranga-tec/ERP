using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceExpenseClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceExpenseClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ServiceJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClaimedByName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FundingSource = table.Column<int>(type: "integer", nullable: false),
                    ExpenseDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MerchantName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ReceiptReference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SettlementPaymentTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    SettledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SettlementReference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceExpenseClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceExpenseClaims_PaymentTypes_SettlementPaymentTypeId",
                        column: x => x.SettlementPaymentTypeId,
                        principalTable: "PaymentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceExpenseClaimLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceExpenseClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BillableToCustomer = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceExpenseClaimLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceExpenseClaimLine_ServiceExpenseClaims_ServiceExpense~",
                        column: x => x.ServiceExpenseClaimId,
                        principalTable: "ServiceExpenseClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceExpenseClaimLine_ItemId",
                table: "ServiceExpenseClaimLine",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceExpenseClaimLine_ServiceExpenseClaimId",
                table: "ServiceExpenseClaimLine",
                column: "ServiceExpenseClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceExpenseClaims_ClaimedByUserId",
                table: "ServiceExpenseClaims",
                column: "ClaimedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceExpenseClaims_Number",
                table: "ServiceExpenseClaims",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceExpenseClaims_ServiceJobId",
                table: "ServiceExpenseClaims",
                column: "ServiceJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceExpenseClaims_SettlementPaymentTypeId",
                table: "ServiceExpenseClaims",
                column: "SettlementPaymentTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceExpenseClaimLine");

            migrationBuilder.DropTable(
                name: "ServiceExpenseClaims");
        }
    }
}
