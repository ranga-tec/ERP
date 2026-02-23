using ISS.Domain.Audit;
using ISS.Domain.Finance;
using ISS.Domain.Inventory;
using ISS.Domain.MasterData;
using ISS.Domain.Procurement;
using ISS.Domain.Sales;
using ISS.Domain.Sequences;
using ISS.Domain.Service;
using ISS.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace ISS.Application.Persistence;

public interface IIssDbContext
{
    DbSet<Brand> Brands { get; }
    DbSet<UnitOfMeasure> UnitOfMeasures { get; }
    DbSet<ItemCategory> ItemCategories { get; }
    DbSet<ItemSubcategory> ItemSubcategories { get; }
    DbSet<Item> Items { get; }
    DbSet<ItemAttachment> ItemAttachments { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<ReorderSetting> ReorderSettings { get; }

    DbSet<InventoryMovement> InventoryMovements { get; }
    DbSet<StockAdjustment> StockAdjustments { get; }
    DbSet<StockTransfer> StockTransfers { get; }

    DbSet<RequestForQuote> RequestForQuotes { get; }
    DbSet<PurchaseRequisition> PurchaseRequisitions { get; }
    DbSet<PurchaseOrder> PurchaseOrders { get; }
    DbSet<GoodsReceipt> GoodsReceipts { get; }
    DbSet<DirectPurchase> DirectPurchases { get; }
    DbSet<SupplierInvoice> SupplierInvoices { get; }
    DbSet<SupplierReturn> SupplierReturns { get; }

    DbSet<SalesQuote> SalesQuotes { get; }
    DbSet<SalesOrder> SalesOrders { get; }
    DbSet<DispatchNote> DispatchNotes { get; }
    DbSet<DirectDispatch> DirectDispatches { get; }
    DbSet<SalesInvoice> SalesInvoices { get; }
    DbSet<CustomerReturn> CustomerReturns { get; }

    DbSet<EquipmentUnit> EquipmentUnits { get; }
    DbSet<ServiceJob> ServiceJobs { get; }
    DbSet<WorkOrder> WorkOrders { get; }
    DbSet<MaterialRequisition> MaterialRequisitions { get; }
    DbSet<QualityCheck> QualityChecks { get; }

    DbSet<AccountsReceivableEntry> AccountsReceivableEntries { get; }
    DbSet<AccountsPayableEntry> AccountsPayableEntries { get; }
    DbSet<Payment> Payments { get; }
    DbSet<CreditNote> CreditNotes { get; }
    DbSet<DebitNote> DebitNotes { get; }
    DbSet<NotificationOutboxItem> NotificationOutboxItems { get; }

    DbSet<AuditLog> AuditLogs { get; }
    DbSet<DocumentSequence> DocumentSequences { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    DbContext DbContext { get; }
}
