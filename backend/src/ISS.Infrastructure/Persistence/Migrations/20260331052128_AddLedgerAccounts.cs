using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLedgerAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LedgerAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AccountType = table.Column<int>(type: "integer", nullable: false),
                    ParentAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    AllowsPosting = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LedgerAccounts_LedgerAccounts_ParentAccountId",
                        column: x => x.ParentAccountId,
                        principalTable: "LedgerAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerAccounts_Code",
                table: "LedgerAccounts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LedgerAccounts_ParentAccountId",
                table: "LedgerAccounts",
                column: "ParentAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LedgerAccounts");
        }
    }
}
