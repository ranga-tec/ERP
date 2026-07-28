using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPettyCashRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PettyCashRequestLineId",
                table: "PettyCashTransaction",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PettyCashRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PettyCashFundId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NeededByAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PettyCashRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PettyCashRequests_PettyCashFunds_PettyCashFundId",
                        column: x => x.PettyCashFundId,
                        principalTable: "PettyCashFunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PettyCashRequestLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PettyCashRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    ServiceJobId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomCategoryName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Purpose = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ApprovedAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PettyCashRequestLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PettyCashRequestLine_PettyCashRequests_PettyCashRequestId",
                        column: x => x.PettyCashRequestId,
                        principalTable: "PettyCashRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PettyCashRequestLineFunding",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PettyCashRequestLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    FundedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PaymentReference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PettyCashRequestLineFunding", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PettyCashRequestLineFunding_PettyCashRequestLine_PettyCashR~",
                        column: x => x.PettyCashRequestLineId,
                        principalTable: "PettyCashRequestLine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashTransaction_PettyCashRequestLineId",
                table: "PettyCashTransaction",
                column: "PettyCashRequestLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashRequestLine_PettyCashRequestId",
                table: "PettyCashRequestLine",
                column: "PettyCashRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashRequestLine_ServiceJobId",
                table: "PettyCashRequestLine",
                column: "ServiceJobId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashRequestLineFunding_PettyCashRequestLineId",
                table: "PettyCashRequestLineFunding",
                column: "PettyCashRequestLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashRequests_Number",
                table: "PettyCashRequests",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashRequests_PettyCashFundId",
                table: "PettyCashRequests",
                column: "PettyCashFundId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashRequests_RequestedByUserId",
                table: "PettyCashRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashRequests_Status",
                table: "PettyCashRequests",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_PettyCashTransaction_PettyCashRequestLine_PettyCashRequestL~",
                table: "PettyCashTransaction",
                column: "PettyCashRequestLineId",
                principalTable: "PettyCashRequestLine",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PettyCashTransaction_PettyCashRequestLine_PettyCashRequestL~",
                table: "PettyCashTransaction");

            migrationBuilder.DropTable(
                name: "PettyCashRequestLineFunding");

            migrationBuilder.DropTable(
                name: "PettyCashRequestLine");

            migrationBuilder.DropTable(
                name: "PettyCashRequests");

            migrationBuilder.DropIndex(
                name: "IX_PettyCashTransaction_PettyCashRequestLineId",
                table: "PettyCashTransaction");

            migrationBuilder.DropColumn(
                name: "PettyCashRequestLineId",
                table: "PettyCashTransaction");
        }
    }
}
