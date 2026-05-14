using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchInstalledBaseFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextServiceDueAt",
                table: "DispatchNotes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceIntervalDays",
                table: "DispatchNotes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarrantyCoverage",
                table: "DispatchNotes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "WarrantyUntil",
                table: "DispatchNotes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextServiceDueAt",
                table: "DirectDispatches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceIntervalDays",
                table: "DirectDispatches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarrantyCoverage",
                table: "DirectDispatches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "WarrantyUntil",
                table: "DirectDispatches",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextServiceDueAt",
                table: "DispatchNotes");

            migrationBuilder.DropColumn(
                name: "ServiceIntervalDays",
                table: "DispatchNotes");

            migrationBuilder.DropColumn(
                name: "WarrantyCoverage",
                table: "DispatchNotes");

            migrationBuilder.DropColumn(
                name: "WarrantyUntil",
                table: "DispatchNotes");

            migrationBuilder.DropColumn(
                name: "NextServiceDueAt",
                table: "DirectDispatches");

            migrationBuilder.DropColumn(
                name: "ServiceIntervalDays",
                table: "DirectDispatches");

            migrationBuilder.DropColumn(
                name: "WarrantyCoverage",
                table: "DirectDispatches");

            migrationBuilder.DropColumn(
                name: "WarrantyUntil",
                table: "DirectDispatches");
        }
    }
}
