using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPettyCashIouApprovalRouting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AssignedApprovedAt",
                table: "PettyCashIous",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedApproverName",
                table: "PettyCashIous",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedApproverUserId",
                table: "PettyCashIous",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AssignedAt",
                table: "PettyCashIous",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HeadOfficeSubmittedAt",
                table: "PettyCashIous",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HeadOfficeSubmittedByUserId",
                table: "PettyCashIous",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewerName",
                table: "PettyCashIous",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewerUserId",
                table: "PettyCashIous",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashIous_AssignedApproverUserId",
                table: "PettyCashIous",
                column: "AssignedApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashIous_ReviewerUserId",
                table: "PettyCashIous",
                column: "ReviewerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PettyCashIous_AssignedApproverUserId",
                table: "PettyCashIous");

            migrationBuilder.DropIndex(
                name: "IX_PettyCashIous_ReviewerUserId",
                table: "PettyCashIous");

            migrationBuilder.DropColumn(
                name: "AssignedApprovedAt",
                table: "PettyCashIous");

            migrationBuilder.DropColumn(
                name: "AssignedApproverName",
                table: "PettyCashIous");

            migrationBuilder.DropColumn(
                name: "AssignedApproverUserId",
                table: "PettyCashIous");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "PettyCashIous");

            migrationBuilder.DropColumn(
                name: "HeadOfficeSubmittedAt",
                table: "PettyCashIous");

            migrationBuilder.DropColumn(
                name: "HeadOfficeSubmittedByUserId",
                table: "PettyCashIous");

            migrationBuilder.DropColumn(
                name: "ReviewerName",
                table: "PettyCashIous");

            migrationBuilder.DropColumn(
                name: "ReviewerUserId",
                table: "PettyCashIous");
        }
    }
}
