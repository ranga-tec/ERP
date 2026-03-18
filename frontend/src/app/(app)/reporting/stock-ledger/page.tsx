import { backendFetchJson } from "@/lib/backend.server";
import { ItemInlineLink } from "@/components/InlineLink";
import { TransactionLink } from "@/components/TransactionLink";
import { Button, Card, Select, Table } from "@/components/ui";

type WarehouseDto = { id: string; code: string; name: string };
type ItemDto = { id: string; sku: string; name: string };

type StockLedgerRow = {
  occurredAt: string;
  movementType: number;
  warehouseId: string;
  warehouseCode: string;
  warehouseName: string;
  itemId: string;
  itemSku: string;
  itemName: string;
  quantity: number;
  unitCost: number;
  lineValue: number;
  runningQuantity: number;
  referenceType: string;
  referenceId: string;
  batchNumber?: string | null;
  serialNumber?: string | null;
};

type StockLedgerReport = {
  from?: string | null;
  to?: string | null;
  warehouseId?: string | null;
  itemId?: string | null;
  count: number;
  netQuantity: number;
  rows: StockLedgerRow[];
};

const movementTypeLabel: Record<number, string> = {
  1: "Receipt",
  2: "Issue",
  3: "Adjustment",
  4: "Transfer In",
  5: "Transfer Out",
  6: "Consumption",
  7: "Supplier Return",
};

function number(value: number) {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 2 }).format(value);
}

function signedNumber(value: number) {
  const formatted = number(Math.abs(value));
  if (value > 0) {
    return `+${formatted}`;
  }

  if (value < 0) {
    return `-${formatted}`;
  }

  return formatted;
}

export default async function StockLedgerPage({
  searchParams,
}: {
  searchParams?: Promise<{ warehouseId?: string; itemId?: string; take?: string }>;
}) {
  const sp = await searchParams;
  const warehouseId = sp?.warehouseId ?? "";
  const itemId = sp?.itemId ?? "";
  const take = Math.min(1000, Math.max(1, Number(sp?.take ?? "200") || 200));

  const qs = new URLSearchParams({ take: String(take) });
  if (warehouseId) qs.set("warehouseId", warehouseId);
  if (itemId) qs.set("itemId", itemId);

  const [warehouses, items, report] = await Promise.all([
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<ItemDto[]>("/items"),
    backendFetchJson<StockLedgerReport>(`/reporting/stock-ledger?${qs.toString()}`),
  ]);

  const sortedWarehouses = warehouses.slice().sort((a, b) => a.code.localeCompare(b.code));
  const sortedItems = items.slice().sort((a, b) => a.sku.localeCompare(b.sku));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Stock Ledger</h1>
        <p className="mt-1 text-sm text-zinc-500">Inventory movement history by warehouse and item with running quantities.</p>
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
              <option value="50">50</option>
              <option value="100">100</option>
              <option value="200">200</option>
              <option value="500">500</option>
              <option value="1000">1000</option>
            </Select>
          </div>

          <div className="self-end">
            <Button type="submit">Apply</Button>
          </div>
        </form>
      </Card>

      <div className="grid gap-4 sm:grid-cols-2">
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Rows</div>
          <div className="mt-2 text-2xl font-semibold">{report.count}</div>
        </Card>
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Net Qty</div>
          <div className="mt-2 text-2xl font-semibold">{number(report.netQuantity)}</div>
        </Card>
      </div>

      <Card className="overflow-auto">
        <Table>
          <thead>
            <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
              <th className="py-2 pr-3">When</th>
              <th className="py-2 pr-3">Wh</th>
              <th className="py-2 pr-3">Item</th>
              <th className="py-2 pr-3">Type</th>
              <th className="py-2 pr-3">Qty</th>
              <th className="py-2 pr-3">Running</th>
              <th className="py-2 pr-3">Unit Cost</th>
              <th className="py-2 pr-3">Value</th>
              <th className="py-2 pr-3">Batch / Serial</th>
              <th className="py-2 pr-3">Ref</th>
            </tr>
          </thead>
          <tbody>
            {report.rows.map((row) => (
              <tr
                key={`${row.referenceId}-${row.occurredAt}-${row.itemId}-${row.warehouseId}`}
                className="border-b border-zinc-100 align-top dark:border-zinc-900"
              >
                <td className="py-2 pr-3 text-xs">{new Date(row.occurredAt).toLocaleString()}</td>
                <td className="py-2 pr-3 text-xs">
                  <div className="font-medium">{row.warehouseCode}</div>
                  <div className="text-zinc-500">{row.warehouseName}</div>
                </td>
                <td className="py-2 pr-3 text-xs">
                  <div className="font-medium">
                    <ItemInlineLink itemId={row.itemId}>{row.itemSku}</ItemInlineLink>
                  </div>
                  <div className="text-zinc-500">
                    <ItemInlineLink itemId={row.itemId}>{row.itemName}</ItemInlineLink>
                  </div>
                </td>
                <td className="py-2 pr-3 text-xs text-zinc-500">{movementTypeLabel[row.movementType] ?? row.movementType}</td>
                <td className="py-2 pr-3 text-xs font-medium">{signedNumber(row.quantity)}</td>
                <td className="py-2 pr-3 text-xs">{number(row.runningQuantity)}</td>
                <td className="py-2 pr-3 text-xs">{number(row.unitCost)}</td>
                <td className="py-2 pr-3 text-xs">{signedNumber(row.lineValue)}</td>
                <td className="py-2 pr-3 text-xs">
                  <div className="font-mono text-zinc-500">{row.batchNumber?.trim() ? row.batchNumber : "-"}</div>
                  <div className="font-mono text-zinc-500">{row.serialNumber?.trim() ? row.serialNumber : "-"}</div>
                </td>
                <td className="py-2 pr-3 text-xs">
                  <div className="font-mono">{row.referenceType}</div>
                  <div className="text-zinc-500">
                    <TransactionLink referenceType={row.referenceType} referenceId={row.referenceId} monospace>
                      {row.referenceId.slice(0, 8)}
                    </TransactionLink>
                  </div>
                </td>
              </tr>
            ))}
            {report.rows.length === 0 ? (
              <tr>
                <td className="py-6 text-sm text-zinc-500" colSpan={10}>
                  No inventory movements found for the selected filter.
                </td>
              </tr>
            ) : null}
          </tbody>
        </Table>
      </Card>
    </div>
  );
}
