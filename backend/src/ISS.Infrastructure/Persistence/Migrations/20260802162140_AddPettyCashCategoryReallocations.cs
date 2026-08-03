using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPettyCashCategoryReallocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PettyCashTransaction_ReferenceType_ReferenceId_PettyCashReq~",
                table: "PettyCashTransaction");

            migrationBuilder.CreateTable(
                name: "PettyCashReallocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PettyCashFundId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePettyCashRequestLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationPettyCashRequestLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_PettyCashReallocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PettyCashReallocations_PettyCashFunds_PettyCashFundId",
                        column: x => x.PettyCashFundId,
                        principalTable: "PettyCashFunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PettyCashReallocations_PettyCashRequestLine_DestinationPett~",
                        column: x => x.DestinationPettyCashRequestLineId,
                        principalTable: "PettyCashRequestLine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PettyCashReallocations_PettyCashRequestLine_SourcePettyCash~",
                        column: x => x.SourcePettyCashRequestLineId,
                        principalTable: "PettyCashRequestLine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashTransaction_ReferenceType_ReferenceId_PettyCashReq~",
                table: "PettyCashTransaction",
                columns: new[] { "ReferenceType", "ReferenceId", "PettyCashRequestLineId" },
                unique: true,
                filter: "\"ReferenceType\" IN ('PCRTN', 'PCRAL')");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashReallocations_DestinationPettyCashRequestLineId",
                table: "PettyCashReallocations",
                column: "DestinationPettyCashRequestLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashReallocations_Number",
                table: "PettyCashReallocations",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashReallocations_PettyCashFundId",
                table: "PettyCashReallocations",
                column: "PettyCashFundId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashReallocations_RequestedByUserId",
                table: "PettyCashReallocations",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashReallocations_SourcePettyCashRequestLineId",
                table: "PettyCashReallocations",
                column: "SourcePettyCashRequestLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashReallocations_Status",
                table: "PettyCashReallocations",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PettyCashReallocations");

            migrationBuilder.DropIndex(
                name: "IX_PettyCashTransaction_ReferenceType_ReferenceId_PettyCashReq~",
                table: "PettyCashTransaction");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashTransaction_ReferenceType_ReferenceId_PettyCashReq~",
                table: "PettyCashTransaction",
                columns: new[] { "ReferenceType", "ReferenceId", "PettyCashRequestLineId" },
                unique: true,
                filter: "\"ReferenceType\" = 'PCRTN'");
        }
    }
}
