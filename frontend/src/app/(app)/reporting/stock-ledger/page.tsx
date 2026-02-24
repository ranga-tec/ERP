import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";

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

function number(value: number) {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 2 }).format(value);
}

export default async function StockLedgerPage() {
  const report = await backendFetchJson<StockLedgerReport>("/reporting/stock-ledger?take=200");

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Stock Ledger</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Recent inventory movements (up to 200 rows) across warehouses and items.
        </p>
      </div>

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
              <th className="py-2 pr-3">Ref</th>
            </tr>
          </thead>
          <tbody>
            {report.rows.map((row) => (
              <tr key={`${row.referenceId}-${row.occurredAt}-${row.itemId}-${row.warehouseId}`} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                <td className="py-2 pr-3 text-xs">{new Date(row.occurredAt).toLocaleString()}</td>
                <td className="py-2 pr-3 text-xs">
                  <div className="font-medium">{row.warehouseCode}</div>
                  <div className="text-zinc-500">{row.warehouseName}</div>
                </td>
                <td className="py-2 pr-3 text-xs">
                  <div className="font-medium">{row.itemSku}</div>
                  <div className="text-zinc-500">{row.itemName}</div>
                </td>
                <td className="py-2 pr-3 text-xs text-zinc-500">{row.movementType}</td>
                <td className="py-2 pr-3 text-xs">{number(row.quantity)}</td>
                <td className="py-2 pr-3 text-xs">{number(row.runningQuantity)}</td>
                <td className="py-2 pr-3 text-xs">{number(row.unitCost)}</td>
                <td className="py-2 pr-3 text-xs">{number(row.lineValue)}</td>
                <td className="py-2 pr-3 text-xs">
                  <div className="font-mono">{row.referenceType}</div>
                  <div className="text-zinc-500">{row.referenceId.slice(0, 8)}</div>
                </td>
              </tr>
            ))}
            {report.rows.length === 0 ? (
              <tr>
                <td className="py-6 text-sm text-zinc-500" colSpan={9}>
                  No inventory movements found for the current filter.
                </td>
              </tr>
            ) : null}
          </tbody>
        </Table>
      </Card>
    </div>
  );
}
