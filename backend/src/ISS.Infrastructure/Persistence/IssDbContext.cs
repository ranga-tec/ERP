using ISS.Application.Abstractions;
using ISS.Application.Persistence;
using ISS.Domain.Audit;
using ISS.Domain.Common;
using ISS.Domain.Finance;
using ISS.Domain.Inventory;
using ISS.Domain.MasterData;
using ISS.Domain.Procurement;
using ISS.Domain.Sales;
using ISS.Domain.Sequences;
using ISS.Domain.Service;
using ISS.Domain.Notifications;
using ISS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ISS.Infrastructure.Persistence;

public sealed class IssDbContext(
    DbContextOptions<IssDbContext> options,
    ICurrentUser currentUser,
    IClock clock) : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IIssDbContext
{
    public DbContext DbContext => this;

    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<ReorderSetting> ReorderSettings => Set<ReorderSetting>();

    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();

    public DbSet<RequestForQuote> RequestForQuotes => Set<RequestForQuote>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<SupplierReturn> SupplierReturns => Set<SupplierReturn>();

    public DbSet<SalesQuote> SalesQuotes => Set<SalesQuote>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<DispatchNote> DispatchNotes => Set<DispatchNote>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();

    public DbSet<EquipmentUnit> EquipmentUnits => Set<EquipmentUnit>();
    public DbSet<ServiceJob> ServiceJobs => Set<ServiceJob>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<MaterialRequisition> MaterialRequisitions => Set<MaterialRequisition>();
    public DbSet<QualityCheck> QualityChecks => Set<QualityCheck>();

    public DbSet<AccountsReceivableEntry> AccountsReceivableEntries => Set<AccountsReceivableEntry>();
    public DbSet<AccountsPayableEntry> AccountsPayableEntries => Set<AccountsPayableEntry>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CreditNote> CreditNotes => Set<CreditNote>();
    public DbSet<DebitNote> DebitNotes => Set<DebitNote>();
    public DbSet<NotificationOutboxItem> NotificationOutboxItems => Set<NotificationOutboxItem>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<DocumentSequence> DocumentSequences => Set<DocumentSequence>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Brand>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(32);
            entity.Property(x => x.Name).HasMaxLength(256);
        });

        builder.Entity<Customer>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(32);
            entity.Property(x => x.Name).HasMaxLength(256);
        });

        builder.Entity<Supplier>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(32);
            entity.Property(x => x.Name).HasMaxLength(256);
        });

        builder.Entity<Warehouse>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(32);
            entity.Property(x => x.Name).HasMaxLength(128);
        });

        builder.Entity<Item>(entity =>
        {
            entity.HasIndex(x => x.Sku).IsUnique();
            entity.HasIndex(x => x.Barcode).IsUnique();
            entity.Property(x => x.Sku).HasMaxLength(64);
            entity.Property(x => x.Name).HasMaxLength(256);
            entity.Property(x => x.UnitOfMeasure).HasMaxLength(32);
            entity.Property(x => x.Barcode).HasMaxLength(128);
            entity.HasOne(x => x.Brand).WithMany().HasForeignKey(x => x.BrandId);
        });

        builder.Entity<ReorderSetting>(entity =>
        {
            entity.HasIndex(x => new { x.WarehouseId, x.ItemId }).IsUnique();
            entity.Property(x => x.ReorderPoint).HasPrecision(18, 4);
            entity.Property(x => x.ReorderQuantity).HasPrecision(18, 4);
            entity.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId);
            entity.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId);
        });

        builder.Entity<InventoryMovement>(entity =>
        {
            entity.Property(x => x.Quantity).HasPrecision(18, 4);
            entity.Property(x => x.UnitCost).HasPrecision(18, 4);
            entity.Property(x => x.ReferenceType).HasMaxLength(64);
            entity.Property(x => x.SerialNumber).HasMaxLength(128);
            entity.Property(x => x.BatchNumber).HasMaxLength(128);

            entity.HasIndex(x => new { x.WarehouseId, x.ItemId, x.OccurredAt });
            entity.HasIndex(x => new { x.ItemId, x.SerialNumber });
            entity.HasIndex(x => new { x.ItemId, x.BatchNumber });
        });

        builder.Entity<StockAdjustment>(entity =>
        {
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Number).HasMaxLength(32);
            entity.Property(x => x.Reason).HasMaxLength(2000);
            entity.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.StockAdjustmentId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<StockAdjustmentLine>(entity =>
        {
            entity.Property(x => x.QuantityDelta).HasPrecision(18, 4);
            entity.Property(x => x.UnitCost).HasPrecision(18, 4);
            entity.Property(x => x.BatchNumber).HasMaxLength(128);
            entity.HasMany(x => x.Serials).WithOne().HasForeignKey(x => x.StockAdjustmentLineId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<StockAdjustmentLineSerial>(entity =>
        {
            entity.Property(x => x.SerialNumber).HasMaxLength(128);
            entity.HasIndex(x => x.SerialNumber);
        });

        builder.Entity<StockTransfer>(entity =>
        {
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Number).HasMaxLength(32);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.StockTransferId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<StockTransferLine>(entity =>
        {
            entity.Property(x => x.Quantity).HasPrecision(18, 4);
            entity.Property(x => x.UnitCost).HasPrecision(18, 4);
            entity.Property(x => x.BatchNumber).HasMaxLength(128);
            entity.HasMany(x => x.Serials).WithOne().HasForeignKey(x => x.StockTransferLineId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<StockTransferLineSerial>(entity =>
        {
            entity.Property(x => x.SerialNumber).HasMaxLength(128);
            entity.HasIndex(x => x.SerialNumber);
        });

        builder.Entity<RequestForQuote>(entity =>
        {
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Number).HasMaxLength(32);
            entity.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.RequestForQuoteId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<RequestForQuoteLine>(entity =>
        {
            entity.Property(x => x.Quantity).HasPrecision(18, 4);
        });

        builder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Number).HasMaxLength(32);
            entity.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<PurchaseOrderLine>(entity =>
        {
            entity.Property(x => x.OrderedQuantity).HasPrecision(18, 4);
            entity.Property(x => x.ReceivedQuantity).HasPrecision(18, 4);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 4);
        });

        builder.Entity<GoodsReceipt>(entity =>
        {
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Number).HasMaxLength(32);
            entity.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.GoodsReceiptId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<GoodsReceiptLine>(entity =>
        {
            entity.Property(x => x.Quantity).HasPrecision(18, 4);
            entity.Property(x => x.UnitCost).HasPrecision(18, 4);
            entity.Property(x => x.BatchNumber).HasMaxLength(128);
            entity.HasMany(x => x.Serials).WithOne().HasForeignKey(x => x.GoodsReceiptLineId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<GoodsReceiptLineSerial>(entity =>
        {
            entity.Property(x => x.SerialNumber).HasMaxLength(128);
            entity.HasIndex(x => x.SerialNumber).IsUnique();
        });

        builder.Entity<SupplierReturn>(entity =>
        {
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Number).HasMaxLength(32);
            entity.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.SupplierReturnId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<SupplierReturnLine>(entity =>
        {
            entity.Property(x => x.Quantity).HasPrecision(18, 4);
            entity.Property(x => x.UnitCost).HasPrecision(18, 4);
            entity.Property(x => x.BatchNumber).HasMaxLength(128);
            entity.HasMany(x => x.Serials).WithOne().HasForeignKey(x => x.SupplierReturnLineId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<SupplierReturnLineSerial>(entity =>
        {
            entity.Property(x => x.SerialNumber).HasMaxLength(128);
            entity.HasIndex(x => x.SerialNumber).IsUnique();
        });

        builder.Entity<SalesQuote>(entity =>
        {
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Number).HasMaxLength(32);
            entity.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.SalesQuoteId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<SalesQuoteLine>(entity =>
        {
            entity.Property(x => x.Quantity).HasPrecision(18, 4);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 4);
        });

        builder.Entity<SalesOrder>(entity =>
        {
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Number).HasMaxLength(32);
            entity.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<SalesOrderLine>(entity =>
        {
            entity.Property(x => x.Quantity).HasPrecision(18, 4);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 4);
        });

        builder.Entity<DispatchNote>(entity =>
        {
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Number).HasMaxLength(32);
            entity.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.DispatchNoteId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<DispatchLine>(entity =>
        {
            entity.Property(x => x.Quantity).HasPrecision(18, 4);
            entity.Property(x => x.BatchNumber).HasMaxLength(128);
            entity.HasMany(x => x.Serials).WithOne().HasForeignKey(x => x.DispatchLineId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<DispatchLineSerial>(entity =>
        {
            entity.Property(x => x.SerialNumber).HasMaxLength(128);
            entity.HasIndex(x => x.SerialNumber).IsUnique();
        });

        builder.Entity<SalesInvoice>(entity =>
        {
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Number).HasMaxLength(32);
            entity.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.SalesInvoiceId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<SalesInvoiceLine>(entity =>
        {
            entity.Property(x => x.Quantity).HasPrecision(18, 4);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 4);
            entity.Property(x => x.DiscountPercent).HasPrecision(18, 4);
            entity.Property(x => x.TaxPercent).HasPrecision(18, 4);
        });

        builder.Entity<EquipmentUnit>(entity =>
        {
            entity.HasIndex(x => x.SerialNumber).IsUnique();
            entity.Property(x => x.SerialNumber).HasMaxLength(128);
        });

        builder.Entity<ServiceJob>(entity =>
        {
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Number).HasMaxLength(32);
            entity.Property(x => x.ProblemDescription).HasMaxLength(2000);
        });

        builder.Entity<WorkOrder>(entity =>
        {
            entity.Property(x => x.Description).HasMaxLength(2000);
        });

        builder.Entity<MaterialRequisition>(entity =>
        {
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Number).HasMaxLength(32);
            entity.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.MaterialRequisitionId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<MaterialRequisitionLine>(entity =>
        {
            entity.Property(x => x.Quantity).HasPrecision(18, 4);
            entity.Property(x => x.BatchNumber).HasMaxLength(128);
            entity.HasMany(x => x.Serials).WithOne().HasForeignKey(x => x.MaterialRequisitionLineId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<MaterialRequisitionLineSerial>(entity =>
        {
            entity.Property(x => x.SerialNumber).HasMaxLength(128);
            entity.HasIndex(x => x.SerialNumber).IsUnique();
        });

        builder.Entity<AccountsReceivableEntry>(entity =>
        {
            entity.Property(x => x.ReferenceType).HasMaxLength(64);
            entity.Property(x => x.Amount).HasPrecision(18, 4);
            entity.Property(x => x.Outstanding).HasPrecision(18, 4);
        });

        builder.Entity<AccountsPayableEntry>(entity =>
        {
            entity.Property(x => x.ReferenceType).HasMaxLength(64);
            entity.Property(x => x.Amount).HasPrecision(18, 4);
            entity.Property(x => x.Outstanding).HasPrecision(18, 4);
        });

        builder.Entity<Payment>(entity =>
        {
            entity.Property(x => x.ReferenceNumber).HasMaxLength(64);
            entity.Property(x => x.Amount).HasPrecision(18, 4);
            entity.HasMany(x => x.Allocations).WithOne().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<PaymentAllocation>(entity =>
        {
            entity.Property(x => x.Amount).HasPrecision(18, 4);
        });

        builder.Entity<CreditNote>(entity =>
        {
            entity.HasIndex(x => x.ReferenceNumber).IsUnique();
            entity.Property(x => x.ReferenceNumber).HasMaxLength(64);
            entity.Property(x => x.SourceReferenceType).HasMaxLength(64);
            entity.Property(x => x.Amount).HasPrecision(18, 4);
            entity.Property(x => x.RemainingAmount).HasPrecision(18, 4);
            entity.HasMany(x => x.Allocations).WithOne().HasForeignKey(x => x.CreditNoteId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<CreditNoteAllocation>(entity =>
        {
            entity.Property(x => x.Amount).HasPrecision(18, 4);
        });

        builder.Entity<DebitNote>(entity =>
        {
            entity.HasIndex(x => x.ReferenceNumber).IsUnique();
            entity.Property(x => x.ReferenceNumber).HasMaxLength(64);
            entity.Property(x => x.SourceReferenceType).HasMaxLength(64);
            entity.Property(x => x.Amount).HasPrecision(18, 4);
        });

        builder.Entity<NotificationOutboxItem>(entity =>
        {
            entity.Property(x => x.Recipient).HasMaxLength(256);
            entity.Property(x => x.Subject).HasMaxLength(256);
            entity.Property(x => x.Body).HasMaxLength(8000);
            entity.Property(x => x.LastError).HasMaxLength(2000);
            entity.Property(x => x.ReferenceType).HasMaxLength(64);
            entity.HasIndex(x => new { x.Status, x.NextAttemptAt });
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.Property(x => x.TableName).HasMaxLength(256);
            entity.Property(x => x.Key).HasMaxLength(256);
        });

        builder.Entity<DocumentSequence>(entity =>
        {
            entity.HasIndex(x => x.DocumentType).IsUnique();
            entity.Property(x => x.DocumentType).HasMaxLength(64);
            entity.Property(x => x.Prefix).HasMaxLength(16);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditing();
        AddAuditLogs();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditing()
    {
        var now = clock.UtcNow;
        var userId = currentUser.UserId;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = userId;
                entry.Entity.LastModifiedAt = now;
                entry.Entity.LastModifiedBy = userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastModifiedAt = now;
                entry.Entity.LastModifiedBy = userId;
            }
        }
    }

    private void AddAuditLogs()
    {
        ChangeTracker.DetectChanges();

        var now = clock.UtcNow;
        var userId = currentUser.UserId;

        var entriesToAudit = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entriesToAudit)
        {
            if (entry.Entity is AuditLog)
            {
                continue;
            }

            if (entry.Entity is IdentityUser<Guid> or IdentityRole<Guid>)
            {
                continue;
            }

            var tableName = entry.Metadata.GetTableName() ?? entry.Metadata.Name;
            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Insert,
                EntityState.Modified => AuditAction.Update,
                EntityState.Deleted => AuditAction.Delete,
                _ => throw new InvalidOperationException("Unsupported audit state.")
            };

            var keyValue = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString()
                           ?? string.Empty;

            var changes = new Dictionary<string, object?>();

            foreach (var prop in entry.Properties)
            {
                if (prop.Metadata.IsPrimaryKey())
                {
                    continue;
                }

                if (prop.Metadata.IsShadowProperty())
                {
                    continue;
                }

                if (entry.State == EntityState.Added)
                {
                    changes[prop.Metadata.Name] = new { old = (object?)null, @new = prop.CurrentValue };
                }
                else if (entry.State == EntityState.Deleted)
                {
                    changes[prop.Metadata.Name] = new { old = prop.OriginalValue, @new = (object?)null };
                }
                else if (entry.State == EntityState.Modified && prop.IsModified)
                {
                    changes[prop.Metadata.Name] = new { old = prop.OriginalValue, @new = prop.CurrentValue };
                }
            }

            if (changes.Count == 0)
            {
                continue;
            }

            var changesJson = JsonSerializer.Serialize(changes);
            AuditLogs.Add(new AuditLog(now, userId, tableName, action, keyValue, changesJson));
        }
    }
}
