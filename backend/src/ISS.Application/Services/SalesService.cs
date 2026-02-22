using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Domain.Common;
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
}
