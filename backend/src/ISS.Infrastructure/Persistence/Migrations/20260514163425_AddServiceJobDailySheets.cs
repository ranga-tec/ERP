using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceJobDailySheets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServiceJobDailySheetId",
                table: "ServiceJobProgressUpdates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceJobDailySheetId",
                table: "ServiceJobMaterialDispositions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceJobDailySheetId",
                table: "ServiceJobAssignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceJobDailySheetId",
                table: "ServiceExpenseClaims",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceJobDailySheetId",
                table: "PettyCashIous",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceJobDailySheetId",
                table: "MaterialRequisitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceJobDailySheets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ServiceJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    SheetDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PreparedByName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SiteLocation = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ShiftName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    WeatherOrSiteCondition = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    WorkPlanned = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    WorkCompleted = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    WorkPending = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ProblemsFound = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CustomerInstructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TechnicianNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SupervisorNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceJobDailySheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceJobDailySheets_ServiceJobs_ServiceJobId",
                        column: x => x.ServiceJobId,
                        principalTable: "ServiceJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobProgressUpdates_ServiceJobDailySheetId",
                table: "ServiceJobProgressUpdates",
                column: "ServiceJobDailySheetId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobMaterialDispositions_ServiceJobDailySheetId",
                table: "ServiceJobMaterialDispositions",
                column: "ServiceJobDailySheetId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobAssignments_ServiceJobDailySheetId",
                table: "ServiceJobAssignments",
                column: "ServiceJobDailySheetId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceExpenseClaims_ServiceJobDailySheetId",
                table: "ServiceExpenseClaims",
                column: "ServiceJobDailySheetId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashIous_ServiceJobDailySheetId",
                table: "PettyCashIous",
                column: "ServiceJobDailySheetId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequisitions_ServiceJobDailySheetId",
                table: "MaterialRequisitions",
                column: "ServiceJobDailySheetId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobDailySheets_Number",
                table: "ServiceJobDailySheets",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobDailySheets_ServiceJobId_SheetDate",
                table: "ServiceJobDailySheets",
                columns: new[] { "ServiceJobId", "SheetDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialRequisitions_ServiceJobDailySheets_ServiceJobDailyS~",
                table: "MaterialRequisitions",
                column: "ServiceJobDailySheetId",
                principalTable: "ServiceJobDailySheets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PettyCashIous_ServiceJobDailySheets_ServiceJobDailySheetId",
                table: "PettyCashIous",
                column: "ServiceJobDailySheetId",
                principalTable: "ServiceJobDailySheets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceExpenseClaims_ServiceJobDailySheets_ServiceJobDailyS~",
                table: "ServiceExpenseClaims",
                column: "ServiceJobDailySheetId",
                principalTable: "ServiceJobDailySheets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceJobAssignments_ServiceJobDailySheets_ServiceJobDaily~",
                table: "ServiceJobAssignments",
                column: "ServiceJobDailySheetId",
                principalTable: "ServiceJobDailySheets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceJobMaterialDispositions_ServiceJobDailySheets_Servic~",
                table: "ServiceJobMaterialDispositions",
                column: "ServiceJobDailySheetId",
                principalTable: "ServiceJobDailySheets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceJobProgressUpdates_ServiceJobDailySheets_ServiceJobD~",
                table: "ServiceJobProgressUpdates",
                column: "ServiceJobDailySheetId",
                principalTable: "ServiceJobDailySheets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialRequisitions_ServiceJobDailySheets_ServiceJobDailyS~",
                table: "MaterialRequisitions");

            migrationBuilder.DropForeignKey(
                name: "FK_PettyCashIous_ServiceJobDailySheets_ServiceJobDailySheetId",
                table: "PettyCashIous");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceExpenseClaims_ServiceJobDailySheets_ServiceJobDailyS~",
                table: "ServiceExpenseClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceJobAssignments_ServiceJobDailySheets_ServiceJobDaily~",
                table: "ServiceJobAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceJobMaterialDispositions_ServiceJobDailySheets_Servic~",
                table: "ServiceJobMaterialDispositions");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceJobProgressUpdates_ServiceJobDailySheets_ServiceJobD~",
                table: "ServiceJobProgressUpdates");

            migrationBuilder.DropTable(
                name: "ServiceJobDailySheets");

            migrationBuilder.DropIndex(
                name: "IX_ServiceJobProgressUpdates_ServiceJobDailySheetId",
                table: "ServiceJobProgressUpdates");

            migrationBuilder.DropIndex(
                name: "IX_ServiceJobMaterialDispositions_ServiceJobDailySheetId",
                table: "ServiceJobMaterialDispositions");

            migrationBuilder.DropIndex(
                name: "IX_ServiceJobAssignments_ServiceJobDailySheetId",
                table: "ServiceJobAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ServiceExpenseClaims_ServiceJobDailySheetId",
                table: "ServiceExpenseClaims");

            migrationBuilder.DropIndex(
                name: "IX_PettyCashIous_ServiceJobDailySheetId",
                table: "PettyCashIous");

            migrationBuilder.DropIndex(
                name: "IX_MaterialRequisitions_ServiceJobDailySheetId",
                table: "MaterialRequisitions");

            migrationBuilder.DropColumn(
                name: "ServiceJobDailySheetId",
                table: "ServiceJobProgressUpdates");

            migrationBuilder.DropColumn(
                name: "ServiceJobDailySheetId",
                table: "ServiceJobMaterialDispositions");

            migrationBuilder.DropColumn(
                name: "ServiceJobDailySheetId",
                table: "ServiceJobAssignments");

            migrationBuilder.DropColumn(
                name: "ServiceJobDailySheetId",
                table: "ServiceExpenseClaims");

            migrationBuilder.DropColumn(
                name: "ServiceJobDailySheetId",
                table: "PettyCashIous");

            migrationBuilder.DropColumn(
                name: "ServiceJobDailySheetId",
                table: "MaterialRequisitions");
        }
    }
}
