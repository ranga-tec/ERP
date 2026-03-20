import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { TransactionLink } from "@/components/TransactionLink";
import { Card, SecondaryLink } from "@/components/ui";
import { GoodsReceiptActions } from "../GoodsReceiptActions";
import { GoodsReceiptDraftLinesTable } from "../GoodsReceiptDraftLinesTable";
import { GoodsReceiptReceiptPlanForm } from "../GoodsReceiptReceiptPlanForm";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";

type GoodsReceiptDto = {
  id: string;
  number: string;
  purchaseOrderId: string;
  warehouseId: string;
  receivedAt: string;
  status: number;
  lines: {
    id: string;
    purchaseOrderLineId?: string | null;
    itemId: string;
    quantity: number;
    unitCost: number;
    batchNumber?: string | null;
    serials: string[];
  }[];
};

type PurchaseOrderSummaryDto = { id: string; number: string };
type WarehouseDto = { id: string; code: string; name: string };
type ItemDto = { id: string; sku: string; name: string; trackingType: number; defaultUnitCost: number };
type GoodsReceiptReceiptPlanDto = {
  lines: {
    purchaseOrderLineId: string;
    itemId: string;
    orderedQuantity: number;
    previouslyReceivedQuantity: number;
    reservedInOtherDraftsQuantity: number;
    availableQuantity: number;
    goodsReceiptLineId?: string | null;
    currentQuantity: number;
    unitCost: number;
    batchNumber?: string | null;
    serials: string[];
  }[];
};

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Voided",
};

export default async function GoodsReceiptDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const grn = await backendFetchJson<GoodsReceiptDto>(`/procurement/goods-receipts/${id}`);
  const isDraft = grn.status === 0;

  const [pos, warehouses, items, receiptPlan] = await Promise.all([
    backendFetchJson<PurchaseOrderSummaryDto[]>("/procurement/purchase-orders?take=500"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<ItemDto[]>("/items"),
    isDraft ? backendFetchJson<GoodsReceiptReceiptPlanDto>(`/procurement/goods-receipts/${id}/receipt-plan`) : Promise.resolve(null),
  ]);

  const poById = new Map(pos.map((p) => [p.id, p]));
  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));

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
            <TransactionLink referenceType="PO" referenceId={grn.purchaseOrderId} monospace>
              {poById.get(grn.purchaseOrderId)?.number ?? grn.purchaseOrderId}
            </TransactionLink>
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
          <div className="mb-3 text-sm font-semibold">Receive From PO</div>
          <GoodsReceiptReceiptPlanForm goodsReceiptId={grn.id} lines={receiptPlan?.lines ?? []} items={items} />
        </Card>
      ) : null}

      <Card>
        <div className="mb-3 text-sm font-semibold">{isDraft ? "Current Draft Lines" : "Posted Lines"}</div>
        <GoodsReceiptDraftLinesTable goodsReceiptId={grn.id} lines={grn.lines} items={items} canEdit={isDraft} />
      </Card>

      <DocumentCollaborationPanel referenceType="GRN" referenceId={id} />
    </div>
  );
}
