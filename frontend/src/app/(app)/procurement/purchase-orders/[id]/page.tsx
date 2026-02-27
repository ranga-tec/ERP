import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, SecondaryLink, Table } from "@/components/ui";
import { PurchaseOrderActions } from "../PurchaseOrderActions";
import { PurchaseOrderLineAddForm } from "../PurchaseOrderLineAddForm";
import { PurchaseOrderLineRow } from "../PurchaseOrderLineRow";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";

type PurchaseOrderDto = {
  id: string;
  number: string;
  supplierId: string;
  orderDate: string;
  status: number;
  total: number;
  lines: {
    id: string;
    itemId: string;
    orderedQuantity: number;
    receivedQuantity: number;
    unitPrice: number;
    lineTotal: number;
  }[];
};

type SupplierDto = { id: string; code: string; name: string };
type ItemDto = { id: string; sku: string; name: string };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Approved",
  2: "Partially Received",
  3: "Closed",
  4: "Cancelled",
};

export default async function PurchaseOrderDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [po, suppliers, items] = await Promise.all([
    backendFetchJson<PurchaseOrderDto>(`/procurement/purchase-orders/${id}`),
    backendFetchJson<SupplierDto[]>("/suppliers"),
    backendFetchJson<ItemDto[]>("/items"),
  ]);

  const supplierById = new Map(suppliers.map((s) => [s.id, s]));
  const itemById = new Map(items.map((i) => [i.id, i]));
  const isDraft = po.status === 0;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/procurement/purchase-orders" className="hover:underline">
            Purchase Orders
          </Link>{" "}
          / <span className="font-mono text-xs">{po.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">PO {po.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>
            Supplier:{" "}
            <span className="font-medium text-zinc-900 dark:text-zinc-100">
              {supplierById.get(po.supplierId)?.code ?? po.supplierId}
            </span>
          </div>
          <div>Status: {statusLabel[po.status] ?? po.status}</div>
          <div>Order date: {new Date(po.orderDate).toLocaleString()}</div>
          <div>Total: {po.total}</div>
        </div>
      </div>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink
            href={`/api/backend/procurement/purchase-orders/${po.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
        <PurchaseOrderActions purchaseOrderId={po.id} canApprove={isDraft && po.lines.length > 0} />
      </Card>

      {isDraft ? (
        <Card>
          <div className="mb-3 text-sm font-semibold">Add line</div>
          <PurchaseOrderLineAddForm purchaseOrderId={po.id} items={items} />
        </Card>
      ) : null}

      <Card>
        <div className="mb-3 text-sm font-semibold">Lines</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Item</th>
                <th className="py-2 pr-3">Ordered</th>
                <th className="py-2 pr-3">Received</th>
                <th className="py-2 pr-3">Unit Price</th>
                <th className="py-2 pr-3">Line Total</th>
                {isDraft ? <th className="py-2 pr-3">Actions</th> : null}
              </tr>
            </thead>
            <tbody>
              {po.lines.map((l) => {
                const item = itemById.get(l.itemId);
                const itemLabel = item ? `${item.sku} - ${item.name}` : l.itemId;
                return (
                  <PurchaseOrderLineRow
                    key={l.id}
                    purchaseOrderId={po.id}
                    line={l}
                    itemLabel={itemLabel}
                    canEdit={isDraft}
                  />
                );
              })}
              {po.lines.length === 0 ? (
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

      <DocumentCollaborationPanel referenceType="PO" referenceId={id} />
    </div>
  );
}
