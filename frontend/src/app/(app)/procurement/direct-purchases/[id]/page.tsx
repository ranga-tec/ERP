import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, SecondaryLink, Table } from "@/components/ui";
import { DirectPurchaseActions } from "../DirectPurchaseActions";
import { DirectPurchaseLineAddForm } from "../DirectPurchaseLineAddForm";
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

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Voided",
};

export default async function DirectPurchaseDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [dp, suppliers, warehouses, items] = await Promise.all([
    backendFetchJson<DirectPurchaseDto>(`/procurement/direct-purchases/${id}`),
    backendFetchJson<SupplierDto[]>("/suppliers"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<ItemDto[]>("/items"),
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
          <DirectPurchaseLineAddForm directPurchaseId={dp.id} items={items} />
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
              </tr>
            </thead>
            <tbody>
              {dp.lines.map((l) => (
                <tr key={l.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3">{itemById.get(l.itemId)?.sku ?? l.itemId}</td>
                  <td className="py-2 pr-3">{l.quantity}</td>
                  <td className="py-2 pr-3">{l.unitPrice}</td>
                  <td className="py-2 pr-3">{l.taxPercent}</td>
                  <td className="py-2 pr-3">{l.lineTotal.toFixed(2)}</td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-500">{l.batchNumber ?? "-"}</td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-500">{l.serials.length ? l.serials.join(", ") : "-"}</td>
                </tr>
              ))}
              {dp.lines.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={7}>
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