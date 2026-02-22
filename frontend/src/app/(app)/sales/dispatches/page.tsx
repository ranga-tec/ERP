import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { DispatchCreateForm } from "./DispatchCreateForm";

type DispatchSummaryDto = {
  id: string;
  number: string;
  salesOrderId: string;
  warehouseId: string;
  dispatchedAt: string;
  status: number;
  lineCount: number;
};

type SalesOrderSummaryDto = { id: string; number: string };
type WarehouseDto = { id: string; code: string; name: string };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Voided",
};

export default async function DispatchesPage() {
  const [dispatches, orders, warehouses] = await Promise.all([
    backendFetchJson<DispatchSummaryDto[]>("/sales/dispatches?take=100"),
    backendFetchJson<SalesOrderSummaryDto[]>("/sales/orders?take=500"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
  ]);

  const orderById = new Map(orders.map((o) => [o.id, o]));
  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Dispatch Notes</h1>
        <p className="mt-1 text-sm text-zinc-500">Pick sales order items from inventory, then post.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <DispatchCreateForm salesOrders={orders} warehouses={warehouses} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Order</th>
                <th className="py-2 pr-3">Warehouse</th>
                <th className="py-2 pr-3">Dispatched</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Lines</th>
              </tr>
            </thead>
            <tbody>
              {dispatches.map((d) => (
                <tr key={d.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/sales/dispatches/${d.id}`}>
                      {d.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-600 dark:text-zinc-400">
                    {orderById.get(d.salesOrderId)?.number ?? d.salesOrderId}
                  </td>
                  <td className="py-2 pr-3">{warehouseById.get(d.warehouseId)?.code ?? d.warehouseId}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(d.dispatchedAt).toLocaleString()}</td>
                  <td className="py-2 pr-3">{statusLabel[d.status] ?? d.status}</td>
                  <td className="py-2 pr-3">{d.lineCount}</td>
                </tr>
              ))}
              {dispatches.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={6}>
                    No dispatch notes yet.
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

