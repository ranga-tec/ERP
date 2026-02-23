import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, SecondaryLink, Table } from "@/components/ui";
import { GoodsReceiptActions } from "../GoodsReceiptActions";
import { GoodsReceiptLineAddForm } from "../GoodsReceiptLineAddForm";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";

type GoodsReceiptDto = {
  id: string;
  number: string;
  purchaseOrderId: string;
  warehouseId: string;
  receivedAt: string;
  status: number;
  lines: { id: string; itemId: string; quantity: number; unitCost: number; batchNumber?: string | null; serials: string[] }[];
};

type PurchaseOrderSummaryDto = { id: string; number: string };
type WarehouseDto = { id: string; code: string; name: string };
type ItemDto = { id: string; sku: string; name: string; trackingType: number; defaultUnitCost: number };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Voided",
};

export default async function GoodsReceiptDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [grn, pos, warehouses, items] = await Promise.all([
    backendFetchJson<GoodsReceiptDto>(`/procurement/goods-receipts/${id}`),
    backendFetchJson<PurchaseOrderSummaryDto[]>("/procurement/purchase-orders?take=500"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<ItemDto[]>("/items"),
  ]);

  const poById = new Map(pos.map((p) => [p.id, p]));
  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));
  const itemById = new Map(items.map((i) => [i.id, i]));
  const isDraft = grn.status === 0;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/procurement/goods-receipts" className="hover:underline">
            GRNs
          </Link>{" "}
          / <span className="font-mono text-xs">{grn.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">GRN {grn.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>
            PO:{" "}
            <span className="font-mono text-xs">
              {poById.get(grn.purchaseOrderId)?.number ?? grn.purchaseOrderId}
            </span>
          </div>
          <div>Warehouse: {warehouseById.get(grn.warehouseId)?.code ?? grn.warehouseId}</div>
          <div>Status: {statusLabel[grn.status] ?? grn.status}</div>
          <div>Received: {new Date(grn.receivedAt).toLocaleString()}</div>
        </div>
      </div>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink
            href={`/api/backend/procurement/goods-receipts/${grn.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
        <GoodsReceiptActions goodsReceiptId={grn.id} canPost={isDraft && grn.lines.length > 0} />
      </Card>

      {isDraft ? (
        <Card>
          <div className="mb-3 text-sm font-semibold">Add line</div>
          <GoodsReceiptLineAddForm goodsReceiptId={grn.id} items={items} />
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
                <th className="py-2 pr-3">Unit Cost</th>
                <th className="py-2 pr-3">Batch</th>
                <th className="py-2 pr-3">Serials</th>
              </tr>
            </thead>
            <tbody>
              {grn.lines.map((l) => (
                <tr key={l.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3">{itemById.get(l.itemId)?.sku ?? l.itemId}</td>
                  <td className="py-2 pr-3">{l.quantity}</td>
                  <td className="py-2 pr-3">{l.unitCost}</td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-500">{l.batchNumber ?? "—"}</td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-500">
                    {l.serials?.length ? l.serials.join(", ") : "—"}
                  </td>
                </tr>
              ))}
              {grn.lines.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={5}>
                    No lines yet.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>

      <DocumentCollaborationPanel referenceType="GRN" referenceId={id} />
    </div>
  );
}