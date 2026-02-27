using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Domain.Common;
using ISS.Domain.Finance;
using ISS.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace ISS.Application.Services;

public sealed class SalesService(
    IIssDbContext dbContext,
    IDocumentNumberService documentNumberService,
    IClock clock,
    InventoryService inventoryService,
    NotificationService notificationService)
{
    public async Task<Guid> CreateQuoteAsync(Guid customerId, DateTimeOffset? validUntil, CancellationToken cancellationToken = default)
    {
        var number = await documentNumberService.NextAsync("SQ", "SQ", cancellationToken);
        var quote = new SalesQuote(number, customerId, clock.UtcNow, validUntil);
        await dbContext.SalesQuotes.AddAsync(quote, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return quote.Id;
    }

    public async Task AddQuoteLineAsync(Guid quoteId, Guid itemId, decimal quantity, decimal unitPrice, CancellationToken cancellationToken = default)
    {
        var quote = await dbContext.SalesQuotes.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == quoteId, cancellationToken)
                    ?? throw new NotFoundException("Sales quote not found.");

        var line = quote.AddLine(itemId, quantity, unitPrice);
        dbContext.DbContext.Add(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateQuoteLineAsync(
        Guid quoteId,
        Guid lineId,
        decimal quantity,
        decimal unitPrice,
        CancellationToken cancellationToken = default)
    {
        var quote = await dbContext.SalesQuotes.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == quoteId, cancellationToken)
                    ?? throw new NotFoundException("Sales quote not found.");

        if (!quote.Lines.Any(x => x.Id == lineId))
        {
            throw new NotFoundException("Sales quote line not found.");
        }

        quote.UpdateLine(lineId, quantity, unitPrice);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveQuoteLineAsync(Guid quoteId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var quote = await dbContext.SalesQuotes.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == quoteId, cancellationToken)
                    ?? throw new NotFoundException("Sales quote not found.");

        var line = quote.Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new NotFoundException("Sales quote line not found.");

        quote.RemoveLine(lineId);
        dbContext.DbContext.Remove(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkQuoteSentAsync(Guid quoteId, CancellationToken cancellationToken = default)
    {
        var quote = await dbContext.SalesQuotes.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == quoteId, cancellationToken)
                    ?? throw new NotFoundException("Sales quote not found.");

        quote.MarkSent();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateOrderAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var number = await documentNumberService.NextAsync("SO", "SO", cancellationToken);
        var order = new SalesOrder(number, customerId, clock.UtcNow);
        await dbContext.SalesOrders.AddAsync(order, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return order.Id;
    }

    public async Task AddOrderLineAsync(Guid orderId, Guid itemId, decimal quantity, decimal unitPrice, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.SalesOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
                    ?? throw new NotFoundException("Sales order not found.");

        var line = order.AddLine(itemId, quantity, unitPrice);
        dbContext.DbContext.Add(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateOrderLineAsync(
        Guid orderId,
        Guid lineId,
        decimal quantity,
        decimal unitPrice,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.SalesOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
                    ?? throw new NotFoundException("Sales order not found.");

        if (!order.Lines.Any(x => x.Id == lineId))
        {
            throw new NotFoundException("Sales order line not found.");
        }

        order.UpdateLine(lineId, quantity, unitPrice);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveOrderLineAsync(Guid orderId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.SalesOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
                    ?? throw new NotFoundException("Sales order not found.");

        var line = order.Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new NotFoundException("Sales order line not found.");

        order.RemoveLine(lineId);
        dbContext.DbContext.Remove(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ConfirmOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.SalesOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
                    ?? throw new NotFoundException("Sales order not found.");

        order.Confirm();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateDispatchAsync(Guid orderId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var number = await documentNumberService.NextAsync("DN", "DN", cancellationToken);
        var dispatch = new DispatchNote(number, orderId, warehouseId, clock.UtcNow);
        await dbContext.DispatchNotes.AddAsync(dispatch, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return dispatch.Id;
    }

    public async Task<Guid> CreateDirectDispatchAsync(
        Guid warehouseId,
        Guid? customerId,
        Guid? serviceJobId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        if (customerId is null && serviceJobId is null)
        {
            throw new DomainValidationException("Direct dispatch requires a customer or service job.");
        }

        if (customerId is not null)
        {
            var customerExists = await dbContext.Customers.AsNoTracking().AnyAsync(x => x.Id == customerId.Value, cancellationToken);
            if (!customerExists)
            {
                throw new NotFoundException("Customer not found.");
            }
        }

        if (serviceJobId is not null)
        {
            var job = await dbContext.ServiceJobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == serviceJobId.Value, cancellationToken)
                      ?? throw new NotFoundException("Service job not found.");

            if (customerId is not null && job.CustomerId != customerId.Value)
            {
                throw new DomainValidationException("Service job customer does not match selected customer.");
            }
        }

        var number = await documentNumberService.NextAsync(ReferenceTypes.DirectDispatch, "DDN", cancellationToken);
        var dispatch = new DirectDispatch(number, warehouseId, clock.UtcNow, customerId, serviceJobId, reason);
        await dbContext.DirectDispatches.AddAsync(dispatch, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return dispatch.Id;
    }

    public async Task AddDispatchLineAsync(
        Guid dispatchId,
        Guid itemId,
        decimal quantity,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        var dispatch = await dbContext.DispatchNotes.Include(x => x.Lines).ThenInclude(l => l.Serials)
                         .FirstOrDefaultAsync(x => x.Id == dispatchId, cancellationToken)
                     ?? throw new NotFoundException("Dispatch note not found.");

        var line = dispatch.AddLine(itemId, quantity, batchNumber);
        if (serialNumbers is { Count: > 0 })
        {
            foreach (var serial in serialNumbers)
            {
                line.AddSerial(serial);
            }
        }

        dbContext.DbContext.Add(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateDispatchLineAsync(
        Guid dispatchId,
        Guid lineId,
        decimal quantity,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        var dispatch = await dbContext.DispatchNotes.Include(x => x.Lines).ThenInclude(l => l.Serials)
                         .FirstOrDefaultAsync(x => x.Id == dispatchId, cancellationToken)
                     ?? throw new NotFoundException("Dispatch note not found.");

        if (!dispatch.Lines.Any(x => x.Id == lineId))
        {
            throw new NotFoundException("Dispatch line not found.");
        }

        dispatch.UpdateLine(lineId, quantity, batchNumber, serialNumbers);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveDispatchLineAsync(Guid dispatchId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var dispatch = await dbContext.DispatchNotes.Include(x => x.Lines).ThenInclude(l => l.Serials)
                         .FirstOrDefaultAsync(x => x.Id == dispatchId, cancellationToken)
                     ?? throw new NotFoundException("Dispatch note not found.");

        var line = dispatch.Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new NotFoundException("Dispatch line not found.");

        dispatch.RemoveLine(lineId);
        dbContext.DbContext.Remove(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddDirectDispatchLineAsync(
        Guid directDispatchId,
        Guid itemId,
        decimal quantity,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        var dispatch = await dbContext.DirectDispatches.Include(x => x.Lines).ThenInclude(l => l.Serials)
                         .FirstOrDefaultAsync(x => x.Id == directDispatchId, cancellationToken)
                     ?? throw new NotFoundException("Direct dispatch not found.");

        var line = dispatch.AddLine(itemId, quantity, batchNumber);
        if (serialNumbers is { Count: > 0 })
        {
            foreach (var serial in serialNumbers)
            {
                line.AddSerial(serial);
            }
        }

        dbContext.DbContext.Add(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateDirectDispatchLineAsync(
        Guid directDispatchId,
        Guid lineId,
        decimal quantity,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        var dispatch = await dbContext.DirectDispatches.Include(x => x.Lines).ThenInclude(l => l.Serials)
                         .FirstOrDefaultAsync(x => x.Id == directDispatchId, cancellationToken)
                     ?? throw new NotFoundException("Direct dispatch not found.");

        if (!dispatch.Lines.Any(x => x.Id == lineId))
        {
            throw new NotFoundException("Direct dispatch line not found.");
        }

        dispatch.UpdateLine(lineId, quantity, batchNumber, serialNumbers);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveDirectDispatchLineAsync(Guid directDispatchId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var dispatch = await dbContext.DirectDispatches.Include(x => x.Lines).ThenInclude(l => l.Serials)
                         .FirstOrDefaultAsync(x => x.Id == directDispatchId, cancellationToken)
                     ?? throw new NotFoundException("Direct dispatch not found.");

        var line = dispatch.Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new NotFoundException("Direct dispatch line not found.");

        dispatch.RemoveLine(lineId);
        dbContext.DbContext.Remove(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task PostDispatchAsync(Guid dispatchId, CancellationToken cancellationToken = default)
    {
        var dispatch = await dbContext.DispatchNotes.Include(x => x.Lines).ThenInclude(l => l.Serials)
                         .FirstOrDefaultAsync(x => x.Id == dispatchId, cancellationToken)
                     ?? throw new NotFoundException("Dispatch note not found.");

        var order = await dbContext.SalesOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == dispatch.SalesOrderId, cancellationToken)
                    ?? throw new NotFoundException("Sales order not found.");

        if (order.Status != SalesOrderStatus.Confirmed)
        {
            throw new DomainValidationException("Sales order must be confirmed before dispatch.");
        }

        var itemIds = dispatch.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToListAsync(cancellationToken);
        var itemById = items.ToDictionary(i => i.Id, i => i);

        dispatch.Post();

        foreach (var line in dispatch.Lines)
        {
            if (!itemById.TryGetValue(line.ItemId, out var item))
            {
                throw new DomainValidationException("Invalid item on dispatch note.");
            }

            await inventoryService.RecordIssueAsync(
                dispatch.DispatchedAt,
                dispatch.WarehouseId,
                item,
                line.Quantity,
                unitCost: item.DefaultUnitCost,
                ReferenceTypes.DispatchNote,
                dispatch.Id,
                line.Id,
                line.BatchNumber,
                line.Serials.Select(s => s.SerialNumber).ToList(),
                cancellationToken);
        }

        order.MarkFulfilled();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task PostDirectDispatchAsync(Guid directDispatchId, CancellationToken cancellationToken = default)
    {
        var dispatch = await dbContext.DirectDispatches.Include(x => x.Lines).ThenInclude(l => l.Serials)
                         .FirstOrDefaultAsync(x => x.Id == directDispatchId, cancellationToken)
                     ?? throw new NotFoundException("Direct dispatch not found.");

        var itemIds = dispatch.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToListAsync(cancellationToken);
        var itemById = items.ToDictionary(i => i.Id, i => i);

        dispatch.Post();

        foreach (var line in dispatch.Lines)
        {
            if (!itemById.TryGetValue(line.ItemId, out var item))
            {
                throw new DomainValidationException("Invalid item on direct dispatch.");
            }

            await inventoryService.RecordIssueAsync(
                dispatch.DispatchedAt,
                dispatch.WarehouseId,
                item,
                line.Quantity,
                unitCost: item.DefaultUnitCost,
                ReferenceTypes.DirectDispatch,
                dispatch.Id,
                line.Id,
                line.BatchNumber,
                line.Serials.Select(s => s.SerialNumber).ToList(),
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateInvoiceAsync(Guid customerId, DateTimeOffset? dueDate, CancellationToken cancellationToken = default)
    {
        var number = await documentNumberService.NextAsync("INV", "INV", cancellationToken);
        var invoice = new SalesInvoice(number, customerId, clock.UtcNow, dueDate);
        await dbContext.SalesInvoices.AddAsync(invoice, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return invoice.Id;
    }

    public async Task AddInvoiceLineAsync(
        Guid invoiceId,
        Guid itemId,
        decimal quantity,
        decimal unitPrice,
        decimal discountPercent,
        decimal taxPercent,
        CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.SalesInvoices.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == invoiceId, cancellationToken)
                      ?? throw new NotFoundException("Sales invoice not found.");

        var line = invoice.AddLine(itemId, quantity, unitPrice, discountPercent, taxPercent);
        dbContext.DbContext.Add(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateInvoiceLineAsync(
        Guid invoiceId,
        Guid lineId,
        decimal quantity,
        decimal unitPrice,
        decimal discountPercent,
        decimal taxPercent,
        CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.SalesInvoices.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == invoiceId, cancellationToken)
                      ?? throw new NotFoundException("Sales invoice not found.");

        if (!invoice.Lines.Any(x => x.Id == lineId))
        {
            throw new NotFoundException("Sales invoice line not found.");
        }

        invoice.UpdateLine(lineId, quantity, unitPrice, discountPercent, taxPercent);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveInvoiceLineAsync(Guid invoiceId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.SalesInvoices.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == invoiceId, cancellationToken)
                      ?? throw new NotFoundException("Sales invoice not found.");

        var line = invoice.Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new NotFoundException("Sales invoice line not found.");

        invoice.RemoveLine(lineId);
        dbContext.DbContext.Remove(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task PostInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.SalesInvoices.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == invoiceId, cancellationToken)
                      ?? throw new NotFoundException("Sales invoice not found.");

        invoice.Post();

        await dbContext.AccountsReceivableEntries.AddAsync(
            new ISS.Domain.Finance.AccountsReceivableEntry(invoice.CustomerId, ReferenceTypes.SalesInvoice, invoice.Id, invoice.Total, clock.UtcNow),
            cancellationToken);

        if (notificationService.Enabled)
        {
            var customer = await dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == invoice.CustomerId, cancellationToken);
            var due = invoice.DueDate is null ? "" : $" Due: {invoice.DueDate:yyyy-MM-dd}.";

            if (!string.IsNullOrWhiteSpace(customer?.Email))
            {
                notificationService.EnqueueEmail(
                    customer.Email!,
                    subject: $"Invoice posted: {invoice.Number}",
                    body: $"Invoice {invoice.Number} has been posted. Total: {invoice.Total:0.00}.{due}",
                    referenceType: ReferenceTypes.SalesInvoice,
                    referenceId: invoice.Id);
            }

            if (!string.IsNullOrWhiteSpace(customer?.Phone))
            {
                notificationService.EnqueueSms(
                    customer.Phone!,
                    body: $"Invoice {invoice.Number} posted. Total {invoice.Total:0.00}.{due}",
                    referenceType: ReferenceTypes.SalesInvoice,
                    referenceId: invoice.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateCustomerReturnAsync(
        Guid customerId,
        Guid warehouseId,
        Guid? salesInvoiceId,
        Guid? dispatchNoteId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var customerExists = await dbContext.Customers.AsNoTracking().AnyAsync(x => x.Id == customerId, cancellationToken);
        if (!customerExists)
        {
            throw new NotFoundException("Customer not found.");
        }

        if (salesInvoiceId is { } invoiceId)
        {
            var invoice = await dbContext.SalesInvoices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == invoiceId, cancellationToken)
                         ?? throw new NotFoundException("Sales invoice not found.");
            if (invoice.CustomerId != customerId)
            {
                throw new DomainValidationException("Sales invoice customer does not match customer return.");
            }
        }

        if (dispatchNoteId is { } dnId)
        {
            var dispatch = await dbContext.DispatchNotes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dnId, cancellationToken)
                           ?? throw new NotFoundException("Dispatch note not found.");
            var order = await dbContext.SalesOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dispatch.SalesOrderId, cancellationToken)
                        ?? throw new NotFoundException("Sales order linked to dispatch note not found.");
            if (order.CustomerId != customerId)
            {
                throw new DomainValidationException("Dispatch note customer does not match customer return.");
            }
        }

        var number = await documentNumberService.NextAsync(ReferenceTypes.CustomerReturn, "CRT", cancellationToken);
        var cr = new CustomerReturn(number, customerId, warehouseId, clock.UtcNow, salesInvoiceId, dispatchNoteId, reason);
        await dbContext.CustomerReturns.AddAsync(cr, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return cr.Id;
    }

    public async Task AddCustomerReturnLineAsync(
        Guid customerReturnId,
        Guid itemId,
        decimal quantity,
        decimal unitPrice,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        var cr = await dbContext.CustomerReturns.Include(x => x.Lines).ThenInclude(l => l.Serials)
                     .FirstOrDefaultAsync(x => x.Id == customerReturnId, cancellationToken)
                 ?? throw new NotFoundException("Customer return not found.");

        var line = cr.AddLine(itemId, quantity, unitPrice, batchNumber);
        if (serialNumbers is { Count: > 0 })
        {
            foreach (var serial in serialNumbers)
            {
                line.AddSerial(serial);
            }
        }

        dbContext.DbContext.Add(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateCustomerReturnLineAsync(
        Guid customerReturnId,
        Guid lineId,
        decimal quantity,
        decimal unitPrice,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        var cr = await dbContext.CustomerReturns.Include(x => x.Lines).ThenInclude(l => l.Serials)
                     .FirstOrDefaultAsync(x => x.Id == customerReturnId, cancellationToken)
                 ?? throw new NotFoundException("Customer return not found.");

        if (!cr.Lines.Any(x => x.Id == lineId))
        {
            throw new NotFoundException("Customer return line not found.");
        }

        cr.UpdateLine(lineId, quantity, unitPrice, batchNumber, serialNumbers);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveCustomerReturnLineAsync(Guid customerReturnId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var cr = await dbContext.CustomerReturns.Include(x => x.Lines).ThenInclude(l => l.Serials)
                     .FirstOrDefaultAsync(x => x.Id == customerReturnId, cancellationToken)
                 ?? throw new NotFoundException("Customer return not found.");

        var line = cr.Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new NotFoundException("Customer return line not found.");

        cr.RemoveLine(lineId);
        dbContext.DbContext.Remove(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task PostCustomerReturnAsync(Guid customerReturnId, CancellationToken cancellationToken = default)
    {
        var cr = await dbContext.CustomerReturns.Include(x => x.Lines).ThenInclude(l => l.Serials)
                     .FirstOrDefaultAsync(x => x.Id == customerReturnId, cancellationToken)
                 ?? throw new NotFoundException("Customer return not found.");

        var itemIds = cr.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToListAsync(cancellationToken);
        var itemById = items.ToDictionary(i => i.Id, i => i);

        cr.Post();

        foreach (var line in cr.Lines)
        {
            if (!itemById.TryGetValue(line.ItemId, out var item))
            {
                throw new DomainValidationException("Invalid item on customer return.");
            }

            await inventoryService.RecordReceiptAsync(
                cr.ReturnDate,
                cr.WarehouseId,
                item,
                line.Quantity,
                item.DefaultUnitCost,
                ReferenceTypes.CustomerReturn,
                cr.Id,
                line.Id,
                line.BatchNumber,
                line.Serials.Select(s => s.SerialNumber).ToList(),
                cancellationToken);
        }

        var amount = cr.Lines.Sum(l => l.Quantity * l.UnitPrice);
        var creditNumber = await documentNumberService.NextAsync(ReferenceTypes.CreditNote, ReferenceTypes.CreditNote, cancellationToken);
        var creditNote = new CreditNote(
            creditNumber,
            CounterpartyType.Customer,
            cr.CustomerId,
            amount,
            clock.UtcNow,
            notes: $"Auto credit for customer return {cr.Number}.",
            sourceReferenceType: ReferenceTypes.CustomerReturn,
            sourceReferenceId: cr.Id);

        await dbContext.CreditNotes.AddAsync(creditNote, cancellationToken);

        var arEntries = await dbContext.AccountsReceivableEntries
            .Where(x => x.CustomerId == cr.CustomerId && x.Outstanding > 0)
            .OrderBy(x => x.PostedAt)
            .ToListAsync(cancellationToken);

        foreach (var ar in arEntries)
        {
            if (creditNote.RemainingAmount <= 0)
            {
                break;
            }

            var allocate = Math.Min(ar.Outstanding, creditNote.RemainingAmount);
            var allocation = creditNote.AllocateToAr(ar.Id, allocate);
            dbContext.DbContext.Add(allocation);
            ar.ApplyPayment(allocate);
            await MarkInvoicePaidIfSettledAsync(ar, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkInvoicePaidIfSettledAsync(AccountsReceivableEntry ar, CancellationToken cancellationToken)
    {
        if (ar.Outstanding > 0 || ar.ReferenceType != ReferenceTypes.SalesInvoice)
        {
            return;
        }

        var invoice = await dbContext.SalesInvoices.FirstOrDefaultAsync(x => x.Id == ar.ReferenceId, cancellationToken);
        invoice?.MarkPaid();
    }
}
