import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, SecondaryLink, Table } from "@/components/ui";
import { DirectPurchaseActions } from "../DirectPurchaseActions";
import { DirectPurchaseLineAddForm } from "../DirectPurchaseLineAddForm";
import { DirectPurchaseLineRow } from "../DirectPurchaseLineRow";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";

type DirectPurchaseDto = {
  id: string;
  number: string;
  supplierId: string;
  warehouseId: string;
  purchasedAt: string;
  status: number;
  remarks?: string | null;
  subtotal: number;
  taxTotal: number;
  grandTotal: number;
  lines: {
    id: string;
    itemId: string;
    quantity: number;
    unitPrice: number;
    taxPercent: number;
    batchNumber?: string | null;
    serials: string[];
    lineSubTotal: number;
    lineTax: number;
    lineTotal: number;
  }[];
};

type SupplierDto = { id: string; code: string; name: string };
type WarehouseDto = { id: string; code: string; name: string };
type ItemDto = { id: string; sku: string; name: string; trackingType: number; defaultUnitCost: number };
type TaxDto = { id: string; code: string; name: string; ratePercent: number; isActive: boolean };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Voided",
};

export default async function DirectPurchaseDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [dp, suppliers, warehouses, items, taxes] = await Promise.all([
    backendFetchJson<DirectPurchaseDto>(`/procurement/direct-purchases/${id}`),
    backendFetchJson<SupplierDto[]>("/suppliers"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<ItemDto[]>("/items"),
    backendFetchJson<TaxDto[]>("/taxes"),
  ]);

  const supplierById = new Map(suppliers.map((s) => [s.id, s]));
  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));
  const itemById = new Map(items.map((i) => [i.id, i]));
  const isDraft = dp.status === 0;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/procurement/direct-purchases" className="hover:underline">
            Direct Purchases
          </Link>{" "}
          / <span className="font-mono text-xs">{dp.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Direct Purchase {dp.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>Supplier: {supplierById.get(dp.supplierId)?.code ?? dp.supplierId}</div>
          <div>Warehouse: {warehouseById.get(dp.warehouseId)?.code ?? dp.warehouseId}</div>
          <div>Status: {statusLabel[dp.status] ?? dp.status}</div>
          <div>Date: {new Date(dp.purchasedAt).toLocaleString()}</div>
        </div>
        <div className="mt-2 text-sm text-zinc-500">
          Subtotal: {dp.subtotal.toFixed(2)} · Tax: {dp.taxTotal.toFixed(2)} · Total: {dp.grandTotal.toFixed(2)}
        </div>
        {dp.remarks ? <div className="mt-2 text-sm text-zinc-500">Remarks: {dp.remarks}</div> : null}
      </div>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink
            href={`/api/backend/procurement/direct-purchases/${dp.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
        <DirectPurchaseActions directPurchaseId={dp.id} canPost={isDraft && dp.lines.length > 0} />
      </Card>

      {isDraft ? (
        <Card>
          <div className="mb-3 text-sm font-semibold">Add line</div>
          <DirectPurchaseLineAddForm directPurchaseId={dp.id} items={items} taxes={taxes} />
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
                <th className="py-2 pr-3">Unit Price</th>
                <th className="py-2 pr-3">Tax %</th>
                <th className="py-2 pr-3">Line Total</th>
                <th className="py-2 pr-3">Batch</th>
                <th className="py-2 pr-3">Serials</th>
                {isDraft ? <th className="py-2 pr-3">Actions</th> : null}
              </tr>
            </thead>
            <tbody>
              {dp.lines.map((l) => {
                const item = itemById.get(l.itemId);
                const itemLabel = item ? `${item.sku} - ${item.name}` : l.itemId;
                return (
                  <DirectPurchaseLineRow
                    key={l.id}
                    directPurchaseId={dp.id}
                    line={l}
                    itemLabel={itemLabel}
                    canEdit={isDraft}
                  />
                );
              })}
              {dp.lines.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={isDraft ? 8 : 7}>
                    No lines yet.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>

      <DocumentCollaborationPanel referenceType="DPR" referenceId={id} />
    </div>
  );
}

