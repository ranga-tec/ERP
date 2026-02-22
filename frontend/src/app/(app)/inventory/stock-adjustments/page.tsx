import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { StockAdjustmentCreateForm } from "./StockAdjustmentCreateForm";

type StockAdjustmentSummaryDto = {
  id: string;
  number: string;
  warehouseId: string;
  adjustedAt: string;
  status: number;
};

type WarehouseDto = { id: string; code: string; name: string };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Voided",
};

export default async function StockAdjustmentsPage() {
  const [adjustments, warehouses] = await Promise.all([
    backendFetchJson<StockAdjustmentSummaryDto[]>("/inventory/stock-adjustments?take=100"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
  ]);

  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Stock Adjustments</h1>
        <p className="mt-1 text-sm text-zinc-500">Adjust inventory up/down (counts, damage, corrections).</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <StockAdjustmentCreateForm warehouses={warehouses} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Warehouse</th>
                <th className="py-2 pr-3">Adjusted</th>
                <th className="py-2 pr-3">Status</th>
              </tr>
            </thead>
            <tbody>
              {adjustments.map((a) => (
                <tr key={a.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/inventory/stock-adjustments/${a.id}`}>
                      {a.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">{warehouseById.get(a.warehouseId)?.code ?? a.warehouseId}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(a.adjustedAt).toLocaleString()}</td>
                  <td className="py-2 pr-3">{statusLabel[a.status] ?? a.status}</td>
                </tr>
              ))}
              {adjustments.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={4}>
                    No stock adjustments yet.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>
    </div>
  );
}

