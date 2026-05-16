using ISS.Api.Security;
using ISS.Application.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/test-data")]
[Authorize(Roles = Roles.Admin)]
public sealed class TestDataCleanupController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record CleanupResponse(string Scope, string Message);

    [HttpPost("clear-purchase-orders")]
    public async Task<ActionResult<CleanupResponse>> ClearPurchaseOrders(CancellationToken cancellationToken)
    {
        await using var tx = await dbContext.DbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.DbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM "DocumentComments" WHERE "ReferenceType" IN ('PO', 'GRN', 'SINV');
            DELETE FROM "DocumentAttachments" WHERE "ReferenceType" IN ('PO', 'GRN', 'SINV');
            DELETE FROM "NotificationOutboxItems" WHERE "ReferenceType" IN ('PO', 'GRN', 'SINV');
            DELETE FROM "InventoryMovements" WHERE "ReferenceType" = 'GRN';
            DELETE FROM "AccountsPayableEntries" WHERE "ReferenceType" IN ('GRN', 'SINV');
            TRUNCATE TABLE "SupplierInvoices" CASCADE;
            TRUNCATE TABLE "GoodsReceipts" CASCADE;
            TRUNCATE TABLE "PurchaseOrders" CASCADE;
            """,
            cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return Ok(new CleanupResponse("purchase-orders", "Cleared purchase orders, dependent GRNs, supplier invoices, AP entries, GRN stock movements, and related document metadata."));
    }

    [HttpPost("clear-goods-receipts")]
    public async Task<ActionResult<CleanupResponse>> ClearGoodsReceipts(CancellationToken cancellationToken)
    {
        await using var tx = await dbContext.DbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.DbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM "DocumentComments" WHERE "ReferenceType" IN ('GRN', 'SINV');
            DELETE FROM "DocumentAttachments" WHERE "ReferenceType" IN ('GRN', 'SINV');
            DELETE FROM "NotificationOutboxItems" WHERE "ReferenceType" IN ('GRN', 'SINV');
            DELETE FROM "InventoryMovements" WHERE "ReferenceType" = 'GRN';
            DELETE FROM "AccountsPayableEntries" WHERE "ReferenceType" IN ('GRN', 'SINV');
            DELETE FROM "SupplierInvoices" WHERE "GoodsReceiptId" IS NOT NULL;
            TRUNCATE TABLE "GoodsReceipts" CASCADE;
            """,
            cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return Ok(new CleanupResponse("goods-receipts", "Cleared GRNs, GRN-linked supplier invoices, AP entries, GRN stock movements, and related document metadata."));
    }

    [HttpPost("zero-stock")]
    public async Task<ActionResult<CleanupResponse>> ZeroStock(CancellationToken cancellationToken)
    {
        await using var tx = await dbContext.DbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.DbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE "InventoryMovements" CASCADE;
            """,
            cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return Ok(new CleanupResponse("zero-stock", "Deleted all inventory movement rows. Stock is now zero, but posted source documents remain for testing review."));
    }

    [HttpPost("clear-service")]
    public async Task<ActionResult<CleanupResponse>> ClearService(CancellationToken cancellationToken)
    {
        await using var tx = await dbContext.DbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.DbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM "DocumentComments" WHERE "ReferenceType" IN ('SC', 'SJ', 'SJDS', 'SJMD', 'SE', 'SH', 'MR', 'SEC', 'WO', 'QC');
            DELETE FROM "DocumentAttachments" WHERE "ReferenceType" IN ('SC', 'SJ', 'SJDS', 'SJMD', 'SE', 'SH', 'MR', 'SEC', 'WO', 'QC');
            DELETE FROM "NotificationOutboxItems" WHERE "ReferenceType" IN ('SC', 'SJ', 'SJDS', 'SJMD', 'SE', 'SH', 'MR', 'SEC', 'WO', 'QC');
            DELETE FROM "PettyCashTransaction"
            WHERE ("ReferenceType" = 'SEC' AND "ReferenceId" IN (SELECT "Id" FROM "ServiceExpenseClaims"))
               OR ("ReferenceType" = 'IOU' AND "ReferenceId" IN (SELECT "Id" FROM "PettyCashIous" WHERE "ServiceJobId" IN (SELECT "Id" FROM "ServiceJobs")));
            DELETE FROM "InventoryMovements" WHERE "ReferenceType" IN ('MR', 'SJMD');
            DELETE FROM "PettyCashIous" WHERE "ServiceJobId" IN (SELECT "Id" FROM "ServiceJobs");
            TRUNCATE TABLE "MaterialRequisitions" CASCADE;
            TRUNCATE TABLE "QualityChecks" CASCADE;
            TRUNCATE TABLE "WorkOrders" CASCADE;
            TRUNCATE TABLE "ServiceHandovers" CASCADE;
            TRUNCATE TABLE "ServiceExpenseClaims" CASCADE;
            TRUNCATE TABLE "ServiceEstimates" CASCADE;
            TRUNCATE TABLE "ServiceJobs" CASCADE;
            TRUNCATE TABLE "ServiceContracts" CASCADE;
            """,
            cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return Ok(new CleanupResponse("service", "Cleared service contracts, jobs, daily sheets, assignments, progress, IOUs, expenses, MRNs, material dispositions, QC, handovers, service stock movements, and related document metadata."));
    }
}
