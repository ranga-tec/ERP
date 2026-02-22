import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, SecondaryLink, Table } from "@/components/ui";
import { DispatchActions } from "../DispatchActions";
import { DispatchLineAddForm } from "../DispatchLineAddForm";

type DispatchDto = {
  id: string;
  number: string;
  salesOrderId: string;
  warehouseId: string;
  dispatchedAt: string;
  status: number;
  lines: { id: string; itemId: string; quantity: number; batchNumber?: string | null; serials: string[] }[];
};

type SalesOrderSummaryDto = { id: string; number: string };
type WarehouseDto = { id: string; code: string; name: string };
type ItemDto = { id: string; sku: string; name: string; trackingType: number };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Voided",
};

export default async function DispatchDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [dispatch, orders, warehouses, items] = await Promise.all([
    backendFetchJson<DispatchDto>(`/sales/dispatches/${id}`),
    backendFetchJson<SalesOrderSummaryDto[]>("/sales/orders?take=500"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<ItemDto[]>("/items"),
  ]);

  const orderById = new Map(orders.map((o) => [o.id, o]));
  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));
  const itemById = new Map(items.map((i) => [i.id, i]));
  const isDraft = dispatch.status === 0;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/sales/dispatches" className="hover:underline">
            Dispatches
          </Link>{" "}
          / <span className="font-mono text-xs">{dispatch.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Dispatch {dispatch.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>
            Order:{" "}
            <span className="font-mono text-xs">
              {orderById.get(dispatch.salesOrderId)?.number ?? dispatch.salesOrderId}
            </span>
          </div>
          <div>Warehouse: {warehouseById.get(dispatch.warehouseId)?.code ?? dispatch.warehouseId}</div>
          <div>Status: {statusLabel[dispatch.status] ?? dispatch.status}</div>
          <div>Date: {new Date(dispatch.dispatchedAt).toLocaleString()}</div>
        </div>
      </div>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink
            href={`/api/backend/sales/dispatches/${dispatch.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
        <DispatchActions dispatchId={dispatch.id} canPost={isDraft && dispatch.lines.length > 0} />
      </Card>

      {isDraft ? (
        <Card>
          <div className="mb-3 text-sm font-semibold">Add line</div>
          <DispatchLineAddForm dispatchId={dispatch.id} items={items} />
        </Card>
      ) : null}

      <Card>
        <div className="mb-3 text-sm font-semibold">Lines</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Item</th>
                <th className="py-2 pr-3">Qty</th>
                <th className="py-2 pr-3">Batch</th>
                <th className="py-2 pr-3">Serials</th>
              </tr>
            </thead>
            <tbody>
              {dispatch.lines.map((l) => (
                <tr key={l.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3">{itemById.get(l.itemId)?.sku ?? l.itemId}</td>
                  <td className="py-2 pr-3">{l.quantity}</td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-500">{l.batchNumber ?? "—"}</td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-500">
                    {l.serials?.length ? l.serials.join(", ") : "—"}
                  </td>
                </tr>
              ))}
              {dispatch.lines.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={4}>
                    No lines yet.
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
