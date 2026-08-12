using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PromoteSettledPettyCashIouBillsToJobCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Repair vouchers created by the IOU accounting screen before settlement began
            // promoting them into job cost. Status 2 is Approved; these vouchers must not be
            // marked Settled because the cash was already issued through the IOU.
            migrationBuilder.Sql(
                """
                UPDATE "ServiceExpenseClaims" AS claim
                SET "Status" = 2,
                    "SubmittedAt" = COALESCE(claim."SubmittedAt", iou."SettledAt", NOW()),
                    "ApprovedAt" = COALESCE(claim."ApprovedAt", iou."SettledAt", NOW())
                FROM "PettyCashIous" AS iou
                WHERE claim."PettyCashIouId" = iou."Id"
                  AND claim."Status" = 0
                  AND iou."Status" IN (4, 7)
                  AND EXISTS (
                      SELECT 1
                      FROM "ServiceExpenseClaimLine" AS line
                      WHERE line."ServiceExpenseClaimId" = claim."Id"
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The previous status cannot be inferred safely after normal application use.
        }
    }
}
