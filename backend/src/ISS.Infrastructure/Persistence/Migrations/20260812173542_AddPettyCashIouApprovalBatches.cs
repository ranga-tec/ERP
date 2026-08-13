using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPettyCashIouApprovalBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PettyCashIouApprovalBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PettyCashFundId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AssignedApproverUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedApproverName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AssignedApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    HeadOfficeSubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    HeadOfficeSubmittedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    HeadOfficeApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    HeadOfficeApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    FundingReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FundingReceivedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    FundingReference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RejectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PettyCashIouApprovalBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PettyCashIouApprovalBatches_PettyCashFunds_PettyCashFundId",
                        column: x => x.PettyCashFundId,
                        principalTable: "PettyCashFunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PettyCashIouApprovalBatchLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PettyCashIouApprovalBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PettyCashIouId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ApprovedAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PettyCashIouApprovalBatchLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PettyCashIouApprovalBatchLines_PettyCashIouApprovalBatches_~",
                        column: x => x.PettyCashIouApprovalBatchId,
                        principalTable: "PettyCashIouApprovalBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PettyCashIouApprovalBatchLines_PettyCashIous_PettyCashIouId",
                        column: x => x.PettyCashIouId,
                        principalTable: "PettyCashIous",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashIouApprovalBatches_AssignedApproverUserId",
                table: "PettyCashIouApprovalBatches",
                column: "AssignedApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashIouApprovalBatches_Number",
                table: "PettyCashIouApprovalBatches",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashIouApprovalBatches_PettyCashFundId",
                table: "PettyCashIouApprovalBatches",
                column: "PettyCashFundId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashIouApprovalBatches_ReviewerUserId",
                table: "PettyCashIouApprovalBatches",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashIouApprovalBatches_Status",
                table: "PettyCashIouApprovalBatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashIouApprovalBatchLines_PettyCashIouApprovalBatchId_~",
                table: "PettyCashIouApprovalBatchLines",
                columns: new[] { "PettyCashIouApprovalBatchId", "PettyCashIouId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashIouApprovalBatchLines_PettyCashIouId",
                table: "PettyCashIouApprovalBatchLines",
                column: "PettyCashIouId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PettyCashIouApprovalBatchLines");

            migrationBuilder.DropTable(
                name: "PettyCashIouApprovalBatches");
        }
    }
}
