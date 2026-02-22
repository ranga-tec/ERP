import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { StockTransferCreateForm } from "./StockTransferCreateForm";

type StockTransferSummaryDto = {
  id: string;
  number: string;
  fromWarehouseId: string;
  toWarehouseId: string;
  transferDate: string;
  status: number;
};

type WarehouseDto = { id: string; code: string; name: string };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Voided",
};

export default async function StockTransfersPage() {
  const [transfers, warehouses] = await Promise.all([
    backendFetchJson<StockTransferSummaryDto[]>("/inventory/stock-transfers?take=100"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
  ]);

  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Stock Transfers</h1>
        <p className="mt-1 text-sm text-zinc-500">Move stock between warehouses.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <StockTransferCreateForm warehouses={warehouses} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">From</th>
                <th className="py-2 pr-3">To</th>
                <th className="py-2 pr-3">Date</th>
                <th className="py-2 pr-3">Status</th>
              </tr>
            </thead>
            <tbody>
              {transfers.map((t) => (
                <tr key={t.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/inventory/stock-transfers/${t.id}`}>
                      {t.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">{warehouseById.get(t.fromWarehouseId)?.code ?? t.fromWarehouseId}</td>
                  <td className="py-2 pr-3">{warehouseById.get(t.toWarehouseId)?.code ?? t.toWarehouseId}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(t.transferDate).toLocaleString()}</td>
                  <td className="py-2 pr-3">{statusLabel[t.status] ?? t.status}</td>
                </tr>
              ))}
              {transfers.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={5}>
                    No stock transfers yet.
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

