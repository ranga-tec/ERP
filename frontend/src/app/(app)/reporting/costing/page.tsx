import { backendFetchJson } from "@/lib/backend.server";
import { Button, Card, Select, Table } from "@/components/ui";

type WarehouseDto = { id: string; code: string; name: string };
type ItemDto = { id: string; sku: string; name: string };

type CostingRowDto = {
  itemId: string;
  itemSku: string;
  itemName: string;
  unitOfMeasure: string;
  defaultUnitCost: number;
  weightedAverageCost?: number | null;
  lastReceiptCost?: number | null;
  lastReceiptAt?: string | null;
  onHandQuantity: number;
  inventoryValue: number;
  costVariancePercent?: number | null;
};

type CostingReportDto = {
  warehouseId?: string | null;
  itemId?: string | null;
  baseCurrencyCode: string;
  count: number;
  totalOnHandQuantity: number;
  totalInventoryValue: number;
  rows: CostingRowDto[];
};

function number(value: number) {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 4 }).format(value);
}

function money(value: number, currencyCode: string) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: currencyCode,
    maximumFractionDigits: 2,
  }).format(value);
}

export default async function CostingPage({
  searchParams,
}: {
  searchParams?: Promise<{ warehouseId?: string; itemId?: string; take?: string }>;
}) {
  const sp = await searchParams;
  const warehouseId = sp?.warehouseId ?? "";
  const itemId = sp?.itemId ?? "";
  const take = Math.min(2000, Math.max(1, Number(sp?.take ?? "500") || 500));

  const qs = new URLSearchParams({ take: String(take) });
  if (warehouseId) qs.set("warehouseId", warehouseId);
  if (itemId) qs.set("itemId", itemId);

  const [warehouses, items, report] = await Promise.all([
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<ItemDto[]>("/items"),
    backendFetchJson<CostingReportDto>(`/reporting/costing?${qs.toString()}`),
  ]);

  const sortedWarehouses = warehouses.slice().sort((a, b) => a.code.localeCompare(b.code));
  const sortedItems = items.slice().sort((a, b) => a.sku.localeCompare(b.sku));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Costing</h1>
        <p className="mt-1 text-sm text-zinc-500">Default vs weighted costs, last receipt rates, and on-hand valuation.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Filter</div>
        <form method="GET" className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <label className="mb-1 block text-sm font-medium">Warehouse</label>
            <Select name="warehouseId" defaultValue={warehouseId}>
              <option value="">All warehouses</option>
              {sortedWarehouses.map((warehouse) => (
                <option key={warehouse.id} value={warehouse.id}>
                  {warehouse.code} - {warehouse.name}
                </option>
              ))}
            </Select>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">Item</label>
            <Select name="itemId" defaultValue={itemId}>
              <option value="">All items</option>
              {sortedItems.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.sku} - {item.name}
                </option>
              ))}
            </Select>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">Take</label>
            <Select name="take" defaultValue={String(take)}>
              <option value="100">100</option>
              <option value="250">250</option>
              <option value="500">500</option>
              <option value="1000">1000</option>
              <option value="2000">2000</option>
            </Select>
          </div>

          <div className="self-end">
            <Button type="submit">Apply</Button>
          </div>
        </form>
      </Card>

      <div className="grid gap-4 sm:grid-cols-3">
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Items</div>
          <div className="mt-2 text-2xl font-semibold">{report.count}</div>
        </Card>

        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Total On Hand</div>
          <div className="mt-2 text-2xl font-semibold">{number(report.totalOnHandQuantity)}</div>
        </Card>

        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Inventory Value ({report.baseCurrencyCode})</div>
          <div className="mt-2 text-2xl font-semibold">{money(report.totalInventoryValue, report.baseCurrencyCode)}</div>
        </Card>
      </div>

      <Card className="overflow-auto">
        <Table>
          <thead>
            <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
              <th className="py-2 pr-3">Item</th>
              <th className="py-2 pr-3">UoM</th>
              <th className="py-2 pr-3">On Hand</th>
              <th className="py-2 pr-3">Default Cost</th>
              <th className="py-2 pr-3">Weighted Avg Cost</th>
              <th className="py-2 pr-3">Last Receipt Cost</th>
              <th className="py-2 pr-3">Last Receipt At</th>
              <th className="py-2 pr-3">Variance %</th>
              <th className="py-2 pr-3">Inventory Value</th>
            </tr>
          </thead>
          <tbody>
            {report.rows.map((row) => (
              <tr key={row.itemId} className="border-b border-zinc-100 dark:border-zinc-900">
                <td className="py-2 pr-3 text-xs">
                  <div className="font-medium">{row.itemSku}</div>
                  <div className="text-zinc-500">{row.itemName}</div>
                </td>
                <td className="py-2 pr-3">{row.unitOfMeasure}</td>
                <td className="py-2 pr-3">{number(row.onHandQuantity)}</td>
                <td className="py-2 pr-3">{money(row.defaultUnitCost, report.baseCurrencyCode)}</td>
                <td className="py-2 pr-3">
                  {row.weightedAverageCost == null ? "-" : money(row.weightedAverageCost, report.baseCurrencyCode)}
                </td>
                <td className="py-2 pr-3">
                  {row.lastReceiptCost == null ? "-" : money(row.lastReceiptCost, report.baseCurrencyCode)}
                </td>
                <td className="py-2 pr-3 text-zinc-500">
                  {row.lastReceiptAt ? new Date(row.lastReceiptAt).toLocaleString() : "-"}
                </td>
                <td className="py-2 pr-3">
                  {row.costVariancePercent == null ? "-" : `${row.costVariancePercent.toFixed(2)}%`}
                </td>
                <td className="py-2 pr-3 font-medium">{money(row.inventoryValue, report.baseCurrencyCode)}</td>
              </tr>
            ))}
            {report.rows.length === 0 ? (
              <tr>
                <td className="py-6 text-sm text-zinc-500" colSpan={9}>
                  No costing rows found for the selected filter.
                </td>
              </tr>
            ) : null}
          </tbody>
        </Table>
      </Card>
    </div>
  );
}
