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

    [HttpPost("clear-sales")]
    public async Task<ActionResult<CleanupResponse>> ClearSales(CancellationToken cancellationToken)
    {
        await using var tx = await dbContext.DbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.DbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM "DocumentComments" WHERE "ReferenceType" IN ('SQ', 'SO', 'DN', 'DDN', 'INV', 'CRTN');
            DELETE FROM "DocumentAttachments" WHERE "ReferenceType" IN ('SQ', 'SO', 'DN', 'DDN', 'INV', 'CRTN');
            DELETE FROM "NotificationOutboxItems" WHERE "ReferenceType" IN ('SQ', 'SO', 'DN', 'DDN', 'INV', 'CRTN');
            DELETE FROM "InventoryMovements" WHERE "ReferenceType" IN ('DN', 'DDN', 'CRTN');
            DELETE FROM "AccountsReceivableEntries" WHERE "ReferenceType" IN ('INV', 'CRTN');
            -- Service documents point at sales invoices without a foreign key, so the link has to be
            -- cleared by hand or those rows keep pointing at invoices that no longer exist.
            UPDATE "WorkOrderTimeEntries" SET "SalesInvoiceId" = NULL, "SalesInvoiceLineId" = NULL, "InvoicedAt" = NULL WHERE "SalesInvoiceId" IS NOT NULL;
            UPDATE "ServiceExpenseClaimLine" SET "SalesInvoiceId" = NULL, "SalesInvoiceLineId" = NULL, "InvoicedAt" = NULL WHERE "SalesInvoiceId" IS NOT NULL;
            TRUNCATE TABLE "CustomerReturns" CASCADE;
            TRUNCATE TABLE "SalesInvoices" CASCADE;
            TRUNCATE TABLE "DirectDispatches" CASCADE;
            TRUNCATE TABLE "DispatchNotes" CASCADE;
            TRUNCATE TABLE "SalesOrders" CASCADE;
            TRUNCATE TABLE "SalesQuotes" CASCADE;
            """,
            cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return Ok(new CleanupResponse("sales", "Cleared sales quotes, orders, dispatches, direct dispatches (AOD), invoices, customer returns, AR entries, sales stock movements, and related document metadata. Invoice links on labour and expense lines were reset."));
    }

    [HttpPost("clear-petty-cash")]
    public async Task<ActionResult<CleanupResponse>> ClearPettyCash(CancellationToken cancellationToken)
    {
        await using var tx = await dbContext.DbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.DbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM "DocumentComments" WHERE "ReferenceType" IN ('PCF', 'IOU', 'PCR', 'PCRTN', 'PCRAL');
            DELETE FROM "DocumentAttachments" WHERE "ReferenceType" IN ('PCF', 'IOU', 'PCR', 'PCRTN', 'PCRAL');
            DELETE FROM "NotificationOutboxItems" WHERE "ReferenceType" IN ('PCF', 'IOU', 'PCR', 'PCRTN', 'PCRAL');
            -- Expense vouchers are service documents and are deliberately kept, so their references
            -- into petty cash are released rather than the vouchers deleted.
            UPDATE "ServiceExpenseClaims"
               SET "PettyCashIouId" = NULL, "PettyCashRequestLineId" = NULL, "SettlementPettyCashFundId" = NULL;
            DELETE FROM "PettyCashTransaction";
            DELETE FROM "PettyCashIous";
            DELETE FROM "PettyCashReallocations";
            DELETE FROM "PettyCashReturns";
            DELETE FROM "PettyCashRequestLineFunding";
            DELETE FROM "PettyCashRequestLine";
            DELETE FROM "PettyCashRequests";
            DELETE FROM "PettyCashFunds";
            """,
            cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return Ok(new CleanupResponse("petty-cash", "Cleared petty cash funds, ledgers, requests, returns, category reallocations, and IOU advances. Expense vouchers are kept, with their petty cash links released."));
    }

    [HttpPost("clear-equipment-units")]
    public async Task<ActionResult<CleanupResponse>> ClearEquipmentUnits(CancellationToken cancellationToken)
    {
        // ServiceJobs.EquipmentUnitId has no foreign key, so nothing stops a job outliving its unit.
        // Refusing here is better than silently leaving jobs pointing at units that no longer exist.
        var jobCount = await dbContext.ServiceJobs.CountAsync(cancellationToken);
        if (jobCount > 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = $"{jobCount} service job(s) still reference equipment units. Run \"Clear Jobs / Service\" first, otherwise those jobs would be left pointing at units that no longer exist.",
            });
        }

        await using var tx = await dbContext.DbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.DbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM "DocumentComments" WHERE "ReferenceType" = 'EUNIT';
            DELETE FROM "DocumentAttachments" WHERE "ReferenceType" = 'EUNIT';
            DELETE FROM "NotificationOutboxItems" WHERE "ReferenceType" = 'EUNIT';
            TRUNCATE TABLE "ServiceContracts" CASCADE;
            TRUNCATE TABLE "EquipmentUnits" CASCADE;
            """,
            cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return Ok(new CleanupResponse("equipment-units", "Cleared equipment units and the service contracts attached to them."));
    }

    [HttpPost("clear-procurement-requests")]
    public async Task<ActionResult<CleanupResponse>> ClearProcurementRequests(CancellationToken cancellationToken)
    {
        await using var tx = await dbContext.DbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.DbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM "DocumentComments" WHERE "ReferenceType" IN ('PR', 'RFQ', 'DPR', 'SR');
            DELETE FROM "DocumentAttachments" WHERE "ReferenceType" IN ('PR', 'RFQ', 'DPR', 'SR');
            DELETE FROM "NotificationOutboxItems" WHERE "ReferenceType" IN ('PR', 'RFQ', 'DPR', 'SR');
            DELETE FROM "InventoryMovements" WHERE "ReferenceType" IN ('DPR', 'SR');
            DELETE FROM "AccountsPayableEntries" WHERE "ReferenceType" IN ('DPR', 'SR');
            TRUNCATE TABLE "SupplierReturns" CASCADE;
            TRUNCATE TABLE "DirectPurchases" CASCADE;
            TRUNCATE TABLE "RequestForQuotes" CASCADE;
            TRUNCATE TABLE "PurchaseRequisitions" CASCADE;
            """,
            cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return Ok(new CleanupResponse("procurement-requests", "Cleared purchase requisitions, RFQs, direct purchases, supplier returns, their stock movements and AP entries, and related document metadata."));
    }

    [HttpPost("clear-inventory-documents")]
    public async Task<ActionResult<CleanupResponse>> ClearInventoryDocuments(CancellationToken cancellationToken)
    {
        await using var tx = await dbContext.DbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.DbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM "DocumentComments" WHERE "ReferenceType" IN ('ADJ', 'TRF');
            DELETE FROM "DocumentAttachments" WHERE "ReferenceType" IN ('ADJ', 'TRF');
            DELETE FROM "NotificationOutboxItems" WHERE "ReferenceType" IN ('ADJ', 'TRF');
            DELETE FROM "InventoryMovements" WHERE "ReferenceType" IN ('ADJ', 'TRF');
            TRUNCATE TABLE "StockTransfers" CASCADE;
            TRUNCATE TABLE "StockAdjustments" CASCADE;
            """,
            cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return Ok(new CleanupResponse("inventory-documents", "Cleared stock adjustments and transfers along with the stock movements they posted."));
    }

    [HttpPost("clear-finance")]
    public async Task<ActionResult<CleanupResponse>> ClearFinance(CancellationToken cancellationToken)
    {
        await using var tx = await dbContext.DbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.DbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM "DocumentComments" WHERE "ReferenceType" IN ('PAY', 'CN', 'DBN');
            DELETE FROM "DocumentAttachments" WHERE "ReferenceType" IN ('PAY', 'CN', 'DBN');
            DELETE FROM "NotificationOutboxItems" WHERE "ReferenceType" IN ('PAY', 'CN', 'DBN');
            TRUNCATE TABLE "PaymentAllocation" CASCADE;
            TRUNCATE TABLE "CreditNoteAllocation" CASCADE;
            TRUNCATE TABLE "Payments" CASCADE;
            TRUNCATE TABLE "CreditNotes" CASCADE;
            TRUNCATE TABLE "DebitNotes" CASCADE;
            TRUNCATE TABLE "AccountsReceivableEntries" CASCADE;
            TRUNCATE TABLE "AccountsPayableEntries" CASCADE;
            """,
            cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return Ok(new CleanupResponse("finance", "Cleared payments, credit notes, debit notes, their allocations, and the AR/AP ledgers."));
    }
}
