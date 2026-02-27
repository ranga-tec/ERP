import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, SecondaryLink, Table } from "@/components/ui";
import { StockAdjustmentActions } from "../StockAdjustmentActions";
import { StockAdjustmentLineAddForm } from "../StockAdjustmentLineAddForm";
import { StockAdjustmentLineRow } from "../StockAdjustmentLineRow";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";

type StockAdjustmentDto = {
  id: string;
  number: string;
  warehouseId: string;
  adjustedAt: string;
  status: number;
  reason?: string | null;
  lines: { id: string; itemId: string; quantityDelta: number; unitCost: number; batchNumber?: string | null; serials: string[] }[];
};

type WarehouseDto = { id: string; code: string; name: string };
type ItemDto = { id: string; sku: string; name: string; trackingType: number; defaultUnitCost: number };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Voided",
};

export default async function StockAdjustmentDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [adj, warehouses, items] = await Promise.all([
    backendFetchJson<StockAdjustmentDto>(`/inventory/stock-adjustments/${id}`),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<ItemDto[]>("/items"),
  ]);

  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));
  const itemById = new Map(items.map((i) => [i.id, i]));
  const isDraft = adj.status === 0;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/inventory/stock-adjustments" className="hover:underline">
            Stock Adjustments
          </Link>{" "}
          / <span className="font-mono text-xs">{adj.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Adjustment {adj.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>Warehouse: {warehouseById.get(adj.warehouseId)?.code ?? adj.warehouseId}</div>
          <div>Status: {statusLabel[adj.status] ?? adj.status}</div>
          <div>Date: {new Date(adj.adjustedAt).toLocaleString()}</div>
        </div>
        {adj.reason ? <div className="mt-2 text-sm text-zinc-500">Reason: {adj.reason}</div> : null}
      </div>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink
            href={`/api/backend/inventory/stock-adjustments/${adj.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
        <StockAdjustmentActions adjustmentId={adj.id} canPost={isDraft && adj.lines.length > 0} canVoid={isDraft} />
      </Card>

      {isDraft ? (
        <Card>
          <div className="mb-3 text-sm font-semibold">Add line</div>
          <StockAdjustmentLineAddForm adjustmentId={adj.id} items={items} />
        </Card>
      ) : null}

      <Card>
        <div className="mb-3 text-sm font-semibold">Lines</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Item</th>
                <th className="py-2 pr-3">Qty delta</th>
                <th className="py-2 pr-3">Unit Cost</th>
                <th className="py-2 pr-3">Batch</th>
                <th className="py-2 pr-3">Serials</th>
                {isDraft ? <th className="py-2 pr-3">Actions</th> : null}
              </tr>
            </thead>
            <tbody>
              {adj.lines.map((l) => {
                const item = itemById.get(l.itemId);
                const itemLabel = item ? `${item.sku} - ${item.name}` : l.itemId;
                return (
                  <StockAdjustmentLineRow
                    key={l.id}
                    adjustmentId={adj.id}
                    line={l}
                    itemLabel={itemLabel}
                    canEdit={isDraft}
                  />
                );
              })}
              {adj.lines.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={isDraft ? 6 : 5}>
                    No lines yet.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>

      <DocumentCollaborationPanel referenceType="ADJ" referenceId={id} />
    </div>
  );
}

