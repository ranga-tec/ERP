using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceJobAssignmentsAndProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceJobAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicianId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmployeeName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Role = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AssignedTask = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    AssignedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    WorkStartAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    WorkEndAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NormalHours = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    OvertimeHours = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DailyWorkDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ApprovalStatus = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_ServiceJobAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceJobAssignments_ServiceJobs_ServiceJobId",
                        column: x => x.ServiceJobId,
                        principalTable: "ServiceJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceJobAssignments_ServiceTechnicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "ServiceTechnicians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceJobProgressUpdates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgressDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    WorkCompleted = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    WorkPending = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ProblemsFound = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AdditionalPartsRequired = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AdditionalLaborRequired = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CustomerInstructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SiteIssues = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TechnicianNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SupervisorNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceJobProgressUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceJobProgressUpdates_ServiceJobs_ServiceJobId",
                        column: x => x.ServiceJobId,
                        principalTable: "ServiceJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobAssignments_ServiceJobId_AssignedDate",
                table: "ServiceJobAssignments",
                columns: new[] { "ServiceJobId", "AssignedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobAssignments_TechnicianId",
                table: "ServiceJobAssignments",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobProgressUpdates_ServiceJobId_ProgressDate",
                table: "ServiceJobProgressUpdates",
                columns: new[] { "ServiceJobId", "ProgressDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceJobAssignments");

            migrationBuilder.DropTable(
                name: "ServiceJobProgressUpdates");
        }
    }
}
