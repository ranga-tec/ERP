using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Persistence;
using ISS.Domain.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/items")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Inventory}")]
public sealed class ItemsController(IIssDbContext dbContext, IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record ItemDto(
        Guid Id,
        string Sku,
        string Name,
        ItemType Type,
        TrackingType TrackingType,
        string UnitOfMeasure,
        Guid? BrandId,
        string? Barcode,
        decimal DefaultUnitCost,
        bool IsActive);

    public sealed record CreateItemRequest(
        string Sku,
        string Name,
        ItemType Type,
        TrackingType TrackingType,
        string UnitOfMeasure,
        Guid? BrandId,
        string? Barcode,
        decimal DefaultUnitCost);

    public sealed record UpdateItemRequest(
        string Sku,
        string Name,
        ItemType Type,
        TrackingType TrackingType,
        string UnitOfMeasure,
        Guid? BrandId,
        string? Barcode,
        decimal DefaultUnitCost,
        bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ItemDto>>> List(CancellationToken cancellationToken)
    {
        var items = await dbContext.Items.AsNoTracking()
            .OrderBy(x => x.Sku)
            .Select(x => new ItemDto(
                x.Id,
                x.Sku,
                x.Name,
                x.Type,
                x.TrackingType,
                x.UnitOfMeasure,
                x.BrandId,
                x.Barcode,
                x.DefaultUnitCost,
                x.IsActive))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ItemDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Items.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ItemDto(
                x.Id,
                x.Sku,
                x.Name,
                x.Type,
                x.TrackingType,
                x.UnitOfMeasure,
                x.BrandId,
                x.Barcode,
                x.DefaultUnitCost,
                x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("{id:guid}/label/pdf")]
    public async Task<ActionResult> LabelPdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.ItemLabel, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpGet("by-barcode/{barcode}")]
    public async Task<ActionResult<ItemDto>> GetByBarcode(string barcode, CancellationToken cancellationToken)
    {
        barcode = barcode.Trim();

        var item = await dbContext.Items.AsNoTracking()
            .Where(x => x.Barcode == barcode)
            .Select(x => new ItemDto(
                x.Id,
                x.Sku,
                x.Name,
                x.Type,
                x.TrackingType,
                x.UnitOfMeasure,
                x.BrandId,
                x.Barcode,
                x.DefaultUnitCost,
                x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ItemDto>> Create(CreateItemRequest request, CancellationToken cancellationToken)
    {
        var item = new Item(
            request.Sku,
            request.Name,
            request.Type,
            request.TrackingType,
            request.UnitOfMeasure,
            request.BrandId,
            request.Barcode,
            request.DefaultUnitCost);

        await dbContext.Items.AddAsync(item, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = item.Id }, new ItemDto(
            item.Id,
            item.Sku,
            item.Name,
            item.Type,
            item.TrackingType,
            item.UnitOfMeasure,
            item.BrandId,
            item.Barcode,
            item.DefaultUnitCost,
            item.IsActive));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ItemDto>> Update(Guid id, UpdateItemRequest request, CancellationToken cancellationToken)
    {
        var item = await dbContext.Items.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        item.Update(
            request.Sku,
            request.Name,
            request.Type,
            request.TrackingType,
            request.UnitOfMeasure,
            request.BrandId,
            request.Barcode,
            request.DefaultUnitCost,
            request.IsActive);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ItemDto(
            item.Id,
            item.Sku,
            item.Name,
            item.Type,
            item.TrackingType,
            item.UnitOfMeasure,
            item.BrandId,
            item.Barcode,
            item.DefaultUnitCost,
            item.IsActive));
    }
}
