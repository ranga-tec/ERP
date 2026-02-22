import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { GoodsReceiptCreateForm } from "./GoodsReceiptCreateForm";

type GoodsReceiptSummaryDto = {
  id: string;
  number: string;
  purchaseOrderId: string;
  warehouseId: string;
  receivedAt: string;
  status: number;
};

type PurchaseOrderSummaryDto = { id: string; number: string };
type WarehouseDto = { id: string; code: string; name: string };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Voided",
};

export default async function GoodsReceiptsPage() {
  const [grns, pos, warehouses] = await Promise.all([
    backendFetchJson<GoodsReceiptSummaryDto[]>("/procurement/goods-receipts?take=100"),
    backendFetchJson<PurchaseOrderSummaryDto[]>("/procurement/purchase-orders?take=500"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
  ]);

  const poById = new Map(pos.map((p) => [p.id, p]));
  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Goods Receipts (GRN)</h1>
        <p className="mt-1 text-sm text-zinc-500">Receive PO items into a warehouse, then post.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <GoodsReceiptCreateForm purchaseOrders={pos} warehouses={warehouses} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">PO</th>
                <th className="py-2 pr-3">Warehouse</th>
                <th className="py-2 pr-3">Received</th>
                <th className="py-2 pr-3">Status</th>
              </tr>
            </thead>
            <tbody>
              {grns.map((g) => (
                <tr key={g.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/procurement/goods-receipts/${g.id}`}>
                      {g.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-600 dark:text-zinc-400">
                    {poById.get(g.purchaseOrderId)?.number ?? g.purchaseOrderId}
                  </td>
                  <td className="py-2 pr-3">{warehouseById.get(g.warehouseId)?.code ?? g.warehouseId}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(g.receivedAt).toLocaleString()}</td>
                  <td className="py-2 pr-3">{statusLabel[g.status] ?? g.status}</td>
                </tr>
              ))}
              {grns.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={5}>
                    No GRNs yet.
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

