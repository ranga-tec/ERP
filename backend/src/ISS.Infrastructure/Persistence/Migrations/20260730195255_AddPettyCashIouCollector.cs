using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPettyCashIouCollector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IssuedToName",
                table: "PettyCashIous",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IssuedToUserId",
                table: "PettyCashIous",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashIous_IssuedToUserId",
                table: "PettyCashIous",
                column: "IssuedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashIous_IssueBillNumber",
                table: "PettyCashIous",
                column: "IssueBillNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PettyCashIous_IssuedToUserId",
                table: "PettyCashIous");

            migrationBuilder.DropIndex(
                name: "IX_PettyCashIous_IssueBillNumber",
                table: "PettyCashIous");

            migrationBuilder.DropColumn(
                name: "IssuedToName",
                table: "PettyCashIous");

            migrationBuilder.DropColumn(
                name: "IssuedToUserId",
                table: "PettyCashIous");
        }
    }
}
