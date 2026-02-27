import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, SecondaryLink, Table } from "@/components/ui";
import { SupplierReturnActions } from "../SupplierReturnActions";
import { SupplierReturnLineAddForm } from "../SupplierReturnLineAddForm";
import { SupplierReturnLineRow } from "../SupplierReturnLineRow";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";

type SupplierReturnDto = {
  id: string;
  number: string;
  supplierId: string;
  warehouseId: string;
  returnDate: string;
  status: number;
  reason?: string | null;
  lines: { id: string; itemId: string; quantity: number; unitCost: number; batchNumber?: string | null; serials: string[] }[];
};

type SupplierDto = { id: string; code: string; name: string };
type WarehouseDto = { id: string; code: string; name: string };
type ItemDto = { id: string; sku: string; name: string; trackingType: number; defaultUnitCost: number };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Voided",
};

export default async function SupplierReturnDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [sr, suppliers, warehouses, items] = await Promise.all([
    backendFetchJson<SupplierReturnDto>(`/procurement/supplier-returns/${id}`),
    backendFetchJson<SupplierDto[]>("/suppliers"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<ItemDto[]>("/items"),
  ]);

  const supplierById = new Map(suppliers.map((s) => [s.id, s]));
  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));
  const itemById = new Map(items.map((i) => [i.id, i]));
  const isDraft = sr.status === 0;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/procurement/supplier-returns" className="hover:underline">
            Supplier Returns
          </Link>{" "}
          / <span className="font-mono text-xs">{sr.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Return {sr.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>
            Supplier:{" "}
            <span className="font-medium text-zinc-900 dark:text-zinc-100">
              {supplierById.get(sr.supplierId)?.code ?? sr.supplierId}
            </span>
          </div>
          <div>Warehouse: {warehouseById.get(sr.warehouseId)?.code ?? sr.warehouseId}</div>
          <div>Status: {statusLabel[sr.status] ?? sr.status}</div>
          <div>Return date: {new Date(sr.returnDate).toLocaleString()}</div>
        </div>
        {sr.reason ? <div className="mt-2 text-sm text-zinc-500">Reason: {sr.reason}</div> : null}
      </div>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink
            href={`/api/backend/procurement/supplier-returns/${sr.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
        <SupplierReturnActions supplierReturnId={sr.id} canPost={isDraft && sr.lines.length > 0} />
      </Card>

      {isDraft ? (
        <Card>
          <div className="mb-3 text-sm font-semibold">Add line</div>
          <SupplierReturnLineAddForm supplierReturnId={sr.id} items={items} />
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
                {isDraft ? <th className="py-2 pr-3">Actions</th> : null}
              </tr>
            </thead>
            <tbody>
              {sr.lines.map((l) => {
                const item = itemById.get(l.itemId);
                const itemLabel = item ? `${item.sku} - ${item.name}` : l.itemId;
                return (
                  <SupplierReturnLineRow
                    key={l.id}
                    supplierReturnId={sr.id}
                    line={l}
                    itemLabel={itemLabel}
                    canEdit={isDraft}
                  />
                );
              })}
              {sr.lines.length === 0 ? (
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

      <DocumentCollaborationPanel referenceType="SR" referenceId={id} />
    </div>
  );
}

