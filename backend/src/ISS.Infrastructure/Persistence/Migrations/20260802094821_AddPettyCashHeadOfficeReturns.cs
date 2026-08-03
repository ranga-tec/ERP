using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPettyCashHeadOfficeReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PettyCashReturns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PettyCashFundId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreparedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreparedByName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PreparedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReceivedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceiptReference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RejectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PettyCashReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PettyCashReturns_PettyCashFunds_PettyCashFundId",
                        column: x => x.PettyCashFundId,
                        principalTable: "PettyCashFunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PettyCashReturnLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PettyCashReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                    PettyCashRequestLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PettyCashReturnLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PettyCashReturnLine_PettyCashRequestLine_PettyCashRequestLi~",
                        column: x => x.PettyCashRequestLineId,
                        principalTable: "PettyCashRequestLine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PettyCashReturnLine_PettyCashReturns_PettyCashReturnId",
                        column: x => x.PettyCashReturnId,
                        principalTable: "PettyCashReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashTransaction_ReferenceType_ReferenceId_PettyCashReq~",
                table: "PettyCashTransaction",
                columns: new[] { "ReferenceType", "ReferenceId", "PettyCashRequestLineId" },
                unique: true,
                filter: "\"ReferenceType\" = 'PCRTN'");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashReturnLine_PettyCashRequestLineId",
                table: "PettyCashReturnLine",
                column: "PettyCashRequestLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashReturnLine_PettyCashReturnId_PettyCashRequestLineId",
                table: "PettyCashReturnLine",
                columns: new[] { "PettyCashReturnId", "PettyCashRequestLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashReturns_Number",
                table: "PettyCashReturns",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashReturns_PettyCashFundId",
                table: "PettyCashReturns",
                column: "PettyCashFundId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashReturns_PreparedByUserId",
                table: "PettyCashReturns",
                column: "PreparedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashReturns_Status",
                table: "PettyCashReturns",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PettyCashReturnLine");

            migrationBuilder.DropTable(
                name: "PettyCashReturns");

            migrationBuilder.DropIndex(
                name: "IX_PettyCashTransaction_ReferenceType_ReferenceId_PettyCashReq~",
                table: "PettyCashTransaction");
        }
    }
}
