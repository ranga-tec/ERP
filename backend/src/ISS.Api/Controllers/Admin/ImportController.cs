using ClosedXML.Excel;
using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.Common;
using ISS.Domain.MasterData;
using ISS.Domain.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ISS.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/import")]
[Authorize(Roles = $"{Roles.Admin}")]
public sealed class ImportController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record ImportResult(
        int BrandsCreated,
        int BrandsUpdated,
        int WarehousesCreated,
        int WarehousesUpdated,
        int SuppliersCreated,
        int SuppliersUpdated,
        int CustomersCreated,
        int CustomersUpdated,
        int ItemsCreated,
        int ItemsUpdated,
        int ReorderSettingsCreated,
        int ReorderSettingsUpdated,
        int EquipmentUnitsCreated,
        int EquipmentUnitsUpdated);

    [HttpGet("template")]
    public ActionResult Template()
    {
        using var workbook = new XLWorkbook();

        AddHeaders(workbook.AddWorksheet("Brands"), ["Code", "Name", "IsActive"]);
        AddHeaders(workbook.AddWorksheet("Warehouses"), ["Code", "Name", "Address", "IsActive"]);
        AddHeaders(workbook.AddWorksheet("Suppliers"), ["Code", "Name", "Phone", "Email", "Address", "IsActive"]);
        AddHeaders(workbook.AddWorksheet("Customers"), ["Code", "Name", "Phone", "Email", "Address", "IsActive"]);
        AddHeaders(workbook.AddWorksheet("Items"), ["Sku", "Name", "Type", "TrackingType", "UnitOfMeasure", "BrandCode", "Barcode", "DefaultUnitCost", "IsActive"]);
        AddHeaders(workbook.AddWorksheet("ReorderSettings"), ["WarehouseCode", "ItemSku", "ReorderPoint", "ReorderQuantity"]);
        AddHeaders(workbook.AddWorksheet("EquipmentUnits"), ["ItemSku", "SerialNumber", "CustomerCode", "PurchasedAt", "WarrantyUntil"]);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return File(
            ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "iss-import-template.xlsx");
    }

    [HttpPost("excel")]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<ImportResult>> ImportExcel([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { error = "Empty file." });
        }

        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);

        var errors = new List<string>();

        var brandByCode = (await dbContext.Brands.ToListAsync(cancellationToken))
            .ToDictionary(x => x.Code, x => x, StringComparer.OrdinalIgnoreCase);
        var warehouseByCode = (await dbContext.Warehouses.ToListAsync(cancellationToken))
            .ToDictionary(x => x.Code, x => x, StringComparer.OrdinalIgnoreCase);
        var supplierByCode = (await dbContext.Suppliers.ToListAsync(cancellationToken))
            .ToDictionary(x => x.Code, x => x, StringComparer.OrdinalIgnoreCase);
        var customerByCode = (await dbContext.Customers.ToListAsync(cancellationToken))
            .ToDictionary(x => x.Code, x => x, StringComparer.OrdinalIgnoreCase);
        var itemBySku = (await dbContext.Items.ToListAsync(cancellationToken))
            .ToDictionary(x => x.Sku, x => x, StringComparer.OrdinalIgnoreCase);
        var reorderByKey = await dbContext.ReorderSettings.ToDictionaryAsync(x => $"{x.WarehouseId:N}:{x.ItemId:N}", x => x, cancellationToken);
        var equipmentBySerial = (await dbContext.EquipmentUnits.ToListAsync(cancellationToken))
            .ToDictionary(x => x.SerialNumber, x => x, StringComparer.OrdinalIgnoreCase);

        var counters = new Counters();

        ImportBrands(workbook, dbContext, brandByCode, counters, errors);
        ImportWarehouses(workbook, dbContext, warehouseByCode, counters, errors);
        ImportSuppliers(workbook, dbContext, supplierByCode, counters, errors);
        ImportCustomers(workbook, dbContext, customerByCode, counters, errors);
        ImportItems(workbook, dbContext, itemBySku, brandByCode, counters, errors);
        ImportReorderSettings(workbook, dbContext, reorderByKey, warehouseByCode, itemBySku, counters, errors);
        ImportEquipmentUnits(workbook, dbContext, equipmentBySerial, itemBySku, customerByCode, counters, errors);

        if (errors.Count > 0)
        {
            return BadRequest(new { errors });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ImportResult(
            counters.BrandsCreated,
            counters.BrandsUpdated,
            counters.WarehousesCreated,
            counters.WarehousesUpdated,
            counters.SuppliersCreated,
            counters.SuppliersUpdated,
            counters.CustomersCreated,
            counters.CustomersUpdated,
            counters.ItemsCreated,
            counters.ItemsUpdated,
            counters.ReorderCreated,
            counters.ReorderUpdated,
            counters.EquipmentCreated,
            counters.EquipmentUpdated));
    }

    private static void AddHeaders(IXLWorksheet ws, IReadOnlyList<string> headers)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();
    }

    private static void ImportBrands(XLWorkbook wb, IIssDbContext dbContext, Dictionary<string, Brand> brandByCode, Counters counters, List<string> errors)
    {
        var ws = GetWorksheetOrNull(wb, "Brands");
        if (ws is null) return;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in DataRows(ws))
        {
            var code = GetString(row, 1);
            var name = GetString(row, 2);
            var isActive = GetBool(row, 3, defaultValue: true);

            if (IsBlankRow(code, name))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                errors.Add($"Brands row {row.RowNumber()}: Code and Name are required.");
                continue;
            }

            if (!seen.Add(code))
            {
                errors.Add($"Brands row {row.RowNumber()}: Duplicate Code '{code}'.");
                continue;
            }

            if (brandByCode.TryGetValue(code, out var existing))
            {
                existing.Update(code, name, isActive);
                counters.BrandsUpdated++;
                continue;
            }

            var created = new Brand(code, name);
            created.Update(code, name, isActive);
            dbContext.Brands.Add(created);
            brandByCode[code] = created;
            counters.BrandsCreated++;
        }
    }

    private static void ImportWarehouses(XLWorkbook wb, IIssDbContext dbContext, Dictionary<string, Warehouse> warehouseByCode, Counters counters, List<string> errors)
    {
        var ws = GetWorksheetOrNull(wb, "Warehouses");
        if (ws is null) return;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in DataRows(ws))
        {
            var code = GetString(row, 1);
            var name = GetString(row, 2);
            var address = GetNullableString(row, 3);
            var isActive = GetBool(row, 4, defaultValue: true);

            if (IsBlankRow(code, name, address))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                errors.Add($"Warehouses row {row.RowNumber()}: Code and Name are required.");
                continue;
            }

            if (!seen.Add(code))
            {
                errors.Add($"Warehouses row {row.RowNumber()}: Duplicate Code '{code}'.");
                continue;
            }

            if (warehouseByCode.TryGetValue(code, out var existing))
            {
                existing.Update(code, name, address, isActive);
                counters.WarehousesUpdated++;
                continue;
            }

            var created = new Warehouse(code, name, address);
            created.Update(code, name, address, isActive);
            dbContext.Warehouses.Add(created);
            warehouseByCode[code] = created;
            counters.WarehousesCreated++;
        }
    }

    private static void ImportSuppliers(XLWorkbook wb, IIssDbContext dbContext, Dictionary<string, Supplier> supplierByCode, Counters counters, List<string> errors)
    {
        var ws = GetWorksheetOrNull(wb, "Suppliers");
        if (ws is null) return;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in DataRows(ws))
        {
            var code = GetString(row, 1);
            var name = GetString(row, 2);
            var phone = GetNullableString(row, 3);
            var email = GetNullableString(row, 4);
            var address = GetNullableString(row, 5);
            var isActive = GetBool(row, 6, defaultValue: true);

            if (IsBlankRow(code, name, phone, email, address))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                errors.Add($"Suppliers row {row.RowNumber()}: Code and Name are required.");
                continue;
            }

            if (!seen.Add(code))
            {
                errors.Add($"Suppliers row {row.RowNumber()}: Duplicate Code '{code}'.");
                continue;
            }

            if (supplierByCode.TryGetValue(code, out var existing))
            {
                existing.Update(code, name, phone, email, address, isActive);
                counters.SuppliersUpdated++;
                continue;
            }

            var created = new Supplier(code, name, phone, email, address);
            created.Update(code, name, phone, email, address, isActive);
            dbContext.Suppliers.Add(created);
            supplierByCode[code] = created;
            counters.SuppliersCreated++;
        }
    }

    private static void ImportCustomers(XLWorkbook wb, IIssDbContext dbContext, Dictionary<string, Customer> customerByCode, Counters counters, List<string> errors)
    {
        var ws = GetWorksheetOrNull(wb, "Customers");
        if (ws is null) return;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in DataRows(ws))
        {
            var code = GetString(row, 1);
            var name = GetString(row, 2);
            var phone = GetNullableString(row, 3);
            var email = GetNullableString(row, 4);
            var address = GetNullableString(row, 5);
            var isActive = GetBool(row, 6, defaultValue: true);

            if (IsBlankRow(code, name, phone, email, address))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                errors.Add($"Customers row {row.RowNumber()}: Code and Name are required.");
                continue;
            }

            if (!seen.Add(code))
            {
                errors.Add($"Customers row {row.RowNumber()}: Duplicate Code '{code}'.");
                continue;
            }

            if (customerByCode.TryGetValue(code, out var existing))
            {
                existing.Update(code, name, phone, email, address, isActive);
                counters.CustomersUpdated++;
                continue;
            }

            var created = new Customer(code, name, phone, email, address);
            created.Update(code, name, phone, email, address, isActive);
            dbContext.Customers.Add(created);
            customerByCode[code] = created;
            counters.CustomersCreated++;
        }
    }

    private static void ImportItems(
        XLWorkbook wb,
        IIssDbContext dbContext,
        Dictionary<string, Item> itemBySku,
        Dictionary<string, Brand> brandByCode,
        Counters counters,
        List<string> errors)
    {
        var ws = GetWorksheetOrNull(wb, "Items");
        if (ws is null) return;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in DataRows(ws))
        {
            var sku = GetString(row, 1);
            var name = GetString(row, 2);
            var typeRaw = GetString(row, 3);
            var trackingRaw = GetString(row, 4);
            var uom = GetString(row, 5);
            var brandCode = GetNullableString(row, 6);
            var barcode = GetNullableString(row, 7);
            var unitCost = GetDecimal(row, 8, defaultValue: 0m);
            var isActive = GetBool(row, 9, defaultValue: true);

            if (IsBlankRow(sku, name, typeRaw, trackingRaw, uom, brandCode, barcode))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(typeRaw) || string.IsNullOrWhiteSpace(trackingRaw) || string.IsNullOrWhiteSpace(uom))
            {
                errors.Add($"Items row {row.RowNumber()}: Sku, Name, Type, TrackingType and UnitOfMeasure are required.");
                continue;
            }

            if (!seen.Add(sku))
            {
                errors.Add($"Items row {row.RowNumber()}: Duplicate Sku '{sku}'.");
                continue;
            }

            if (!TryParseEnum<ItemType>(typeRaw, out var itemType))
            {
                errors.Add($"Items row {row.RowNumber()}: Invalid Type '{typeRaw}'.");
                continue;
            }

            if (!TryParseEnum<TrackingType>(trackingRaw, out var trackingType))
            {
                errors.Add($"Items row {row.RowNumber()}: Invalid TrackingType '{trackingRaw}'.");
                continue;
            }

            Guid? brandId = null;
            if (!string.IsNullOrWhiteSpace(brandCode))
            {
                if (!brandByCode.TryGetValue(brandCode, out var brand))
                {
                    errors.Add($"Items row {row.RowNumber()}: BrandCode '{brandCode}' not found.");
                    continue;
                }
                brandId = brand.Id;
            }

            if (itemBySku.TryGetValue(sku, out var existing))
            {
                existing.Update(sku, name, itemType, trackingType, uom, brandId, barcode, unitCost, isActive);
                counters.ItemsUpdated++;
                continue;
            }

            var created = new Item(sku, name, itemType, trackingType, uom, brandId, barcode, unitCost);
            created.Update(sku, name, itemType, trackingType, uom, brandId, barcode, unitCost, isActive);
            dbContext.Items.Add(created);
            itemBySku[sku] = created;
            counters.ItemsCreated++;
        }
    }

    private static void ImportReorderSettings(
        XLWorkbook wb,
        IIssDbContext dbContext,
        Dictionary<string, ReorderSetting> reorderByKey,
        Dictionary<string, Warehouse> warehouseByCode,
        Dictionary<string, Item> itemBySku,
        Counters counters,
        List<string> errors)
    {
        var ws = GetWorksheetOrNull(wb, "ReorderSettings");
        if (ws is null) return;

        foreach (var row in DataRows(ws))
        {
            var warehouseCode = GetString(row, 1);
            var itemSku = GetString(row, 2);

            if (IsBlankRow(warehouseCode, itemSku))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(warehouseCode) || string.IsNullOrWhiteSpace(itemSku))
            {
                errors.Add($"ReorderSettings row {row.RowNumber()}: WarehouseCode and ItemSku are required.");
                continue;
            }

            if (!warehouseByCode.TryGetValue(warehouseCode, out var warehouse))
            {
                errors.Add($"ReorderSettings row {row.RowNumber()}: WarehouseCode '{warehouseCode}' not found.");
                continue;
            }

            if (!itemBySku.TryGetValue(itemSku, out var item))
            {
                errors.Add($"ReorderSettings row {row.RowNumber()}: ItemSku '{itemSku}' not found.");
                continue;
            }

            var reorderPoint = GetDecimal(row, 3, defaultValue: 0m);
            var reorderQty = GetDecimal(row, 4, defaultValue: 0m);

            try
            {
                var key = $"{warehouse.Id:N}:{item.Id:N}";
                if (reorderByKey.TryGetValue(key, out var existing))
                {
                    existing.Update(reorderPoint, reorderQty);
                    counters.ReorderUpdated++;
                    continue;
                }

                var created = new ReorderSetting(warehouse.Id, item.Id, reorderPoint, reorderQty);
                dbContext.ReorderSettings.Add(created);
                reorderByKey[key] = created;
                counters.ReorderCreated++;
            }
            catch (DomainValidationException ex)
            {
                errors.Add($"ReorderSettings row {row.RowNumber()}: {ex.Message}");
            }
        }
    }

    private static void ImportEquipmentUnits(
        XLWorkbook wb,
        IIssDbContext dbContext,
        Dictionary<string, EquipmentUnit> equipmentBySerial,
        Dictionary<string, Item> itemBySku,
        Dictionary<string, Customer> customerByCode,
        Counters counters,
        List<string> errors)
    {
        var ws = GetWorksheetOrNull(wb, "EquipmentUnits");
        if (ws is null) return;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in DataRows(ws))
        {
            var itemSku = GetString(row, 1);
            var serial = GetString(row, 2);
            var customerCode = GetString(row, 3);
            var purchasedAt = GetDateTimeOffset(row, 4);
            var warrantyUntil = GetDateTimeOffset(row, 5);

            if (IsBlankRow(itemSku, serial, customerCode))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(itemSku) || string.IsNullOrWhiteSpace(serial) || string.IsNullOrWhiteSpace(customerCode))
            {
                errors.Add($"EquipmentUnits row {row.RowNumber()}: ItemSku, SerialNumber and CustomerCode are required.");
                continue;
            }

            if (!seen.Add(serial))
            {
                errors.Add($"EquipmentUnits row {row.RowNumber()}: Duplicate SerialNumber '{serial}'.");
                continue;
            }

            if (!itemBySku.TryGetValue(itemSku, out var item))
            {
                errors.Add($"EquipmentUnits row {row.RowNumber()}: ItemSku '{itemSku}' not found.");
                continue;
            }

            if (!customerByCode.TryGetValue(customerCode, out var customer))
            {
                errors.Add($"EquipmentUnits row {row.RowNumber()}: CustomerCode '{customerCode}' not found.");
                continue;
            }

            if (equipmentBySerial.TryGetValue(serial, out var existing))
            {
                existing.Update(customer.Id, purchasedAt, warrantyUntil);
                counters.EquipmentUpdated++;
                continue;
            }

            var created = new EquipmentUnit(item.Id, serial, customer.Id, purchasedAt, warrantyUntil);
            dbContext.EquipmentUnits.Add(created);
            equipmentBySerial[serial] = created;
            counters.EquipmentCreated++;
        }
    }

    private static IXLWorksheet? GetWorksheetOrNull(XLWorkbook wb, string name)
        => wb.Worksheets.FirstOrDefault(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<IXLRow> DataRows(IXLWorksheet ws)
    {
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 2; r <= lastRow; r++)
        {
            var row = ws.Row(r);
            if (row.CellsUsed().Any())
            {
                yield return row;
            }
        }
    }

    private static bool IsBlankRow(params string?[] values) => values.All(string.IsNullOrWhiteSpace);

    private static string GetString(IXLRow row, int col)
        => row.Cell(col).GetString().Trim();

    private static string? GetNullableString(IXLRow row, int col)
    {
        var value = row.Cell(col).GetString().Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static decimal GetDecimal(IXLRow row, int col, decimal defaultValue)
    {
        var cell = row.Cell(col);
        if (cell.IsEmpty())
        {
            return defaultValue;
        }

        if (cell.DataType == XLDataType.Number)
        {
            return cell.GetValue<decimal>();
        }

        var text = cell.GetString().Trim();
        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

    private static bool GetBool(IXLRow row, int col, bool defaultValue)
    {
        var cell = row.Cell(col);
        if (cell.IsEmpty())
        {
            return defaultValue;
        }

        if (cell.DataType == XLDataType.Boolean)
        {
            return cell.GetValue<bool>();
        }

        var raw = cell.GetString().Trim();
        if (bool.TryParse(raw, out var b))
        {
            return b;
        }

        return raw switch
        {
            "1" => true,
            "0" => false,
            "Y" or "y" or "YES" or "Yes" or "yes" => true,
            "N" or "n" or "NO" or "No" or "no" => false,
            _ => defaultValue
        };
    }

    private static DateTimeOffset? GetDateTimeOffset(IXLRow row, int col)
    {
        var cell = row.Cell(col);
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.DataType == XLDataType.DateTime)
        {
            var dt = cell.GetDateTime();
            dt = dt.Kind switch
            {
                DateTimeKind.Utc => dt,
                DateTimeKind.Local => dt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
            };
            return new DateTimeOffset(dt);
        }

        var raw = cell.GetString().Trim();
        if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
        {
            return dto.ToUniversalTime();
        }

        if (DateTimeOffset.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out dto))
        {
            return dto.ToUniversalTime();
        }

        return null;
    }

    private static bool TryParseEnum<TEnum>(string raw, out TEnum value) where TEnum : struct, Enum
    {
        raw = raw.Trim();
        if (int.TryParse(raw, out var i))
        {
            if (Enum.IsDefined(typeof(TEnum), i))
            {
                value = (TEnum)Enum.ToObject(typeof(TEnum), i);
                return true;
            }
        }

        return Enum.TryParse(raw, ignoreCase: true, out value);
    }

    private sealed class Counters
    {
        public int BrandsCreated;
        public int BrandsUpdated;
        public int WarehousesCreated;
        public int WarehousesUpdated;
        public int SuppliersCreated;
        public int SuppliersUpdated;
        public int CustomersCreated;
        public int CustomersUpdated;
        public int ItemsCreated;
        public int ItemsUpdated;
        public int ReorderCreated;
        public int ReorderUpdated;
        public int EquipmentCreated;
        public int EquipmentUpdated;
    }
}
