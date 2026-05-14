using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceJobLifecycleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ActualStartAt",
                table: "ServiceJobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerComplaint",
                table: "ServiceJobs",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EstimatedStartAt",
                table: "ServiceJobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternalRemarks",
                table: "ServiceJobs",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobDescription",
                table: "ServiceJobs",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsibleOfficerName",
                table: "ServiceJobs",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "ServiceJobs" SET "Status" = 14 WHERE "Status" = 4;
                UPDATE "ServiceJobs" SET "Status" = 12 WHERE "Status" = 3;
                UPDATE "ServiceJobs" SET "Status" = 7 WHERE "Status" = 2;
                UPDATE "ServiceJobs" SET "Status" = 3 WHERE "Status" = 1;
                UPDATE "ServiceJobs" SET "Status" = 1 WHERE "Status" = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "ServiceJobs" SET "Status" = 0 WHERE "Status" = 1;
                UPDATE "ServiceJobs" SET "Status" = 1 WHERE "Status" = 3;
                UPDATE "ServiceJobs" SET "Status" = 2 WHERE "Status" = 7;
                UPDATE "ServiceJobs" SET "Status" = 3 WHERE "Status" = 12;
                UPDATE "ServiceJobs" SET "Status" = 4 WHERE "Status" = 14;
                """);

            migrationBuilder.DropColumn(
                name: "ActualStartAt",
                table: "ServiceJobs");

            migrationBuilder.DropColumn(
                name: "CustomerComplaint",
                table: "ServiceJobs");

            migrationBuilder.DropColumn(
                name: "EstimatedStartAt",
                table: "ServiceJobs");

            migrationBuilder.DropColumn(
                name: "InternalRemarks",
                table: "ServiceJobs");

            migrationBuilder.DropColumn(
                name: "JobDescription",
                table: "ServiceJobs");

            migrationBuilder.DropColumn(
                name: "ResponsibleOfficerName",
                table: "ServiceJobs");
        }
    }
}
