import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, SecondaryLink, Table } from "@/components/ui";
import { StockTransferActions } from "../StockTransferActions";
import { StockTransferLineAddForm } from "../StockTransferLineAddForm";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";

type StockTransferDto = {
  id: string;
  number: string;
  fromWarehouseId: string;
  toWarehouseId: string;
  transferDate: string;
  status: number;
  notes?: string | null;
  lines: { id: string; itemId: string; quantity: number; unitCost: number; batchNumber?: string | null; serials: string[] }[];
};

type WarehouseDto = { id: string; code: string; name: string };
type ItemDto = { id: string; sku: string; name: string; trackingType: number; defaultUnitCost: number };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Voided",
};

export default async function StockTransferDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [transfer, warehouses, items] = await Promise.all([
    backendFetchJson<StockTransferDto>(`/inventory/stock-transfers/${id}`),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<ItemDto[]>("/items"),
  ]);

  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));
  const itemById = new Map(items.map((i) => [i.id, i]));
  const isDraft = transfer.status === 0;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/inventory/stock-transfers" className="hover:underline">
            Stock Transfers
          </Link>{" "}
          / <span className="font-mono text-xs">{transfer.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Transfer {transfer.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>From: {warehouseById.get(transfer.fromWarehouseId)?.code ?? transfer.fromWarehouseId}</div>
          <div>To: {warehouseById.get(transfer.toWarehouseId)?.code ?? transfer.toWarehouseId}</div>
          <div>Status: {statusLabel[transfer.status] ?? transfer.status}</div>
          <div>Date: {new Date(transfer.transferDate).toLocaleString()}</div>
        </div>
        {transfer.notes ? <div className="mt-2 text-sm text-zinc-500">Notes: {transfer.notes}</div> : null}
      </div>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink
            href={`/api/backend/inventory/stock-transfers/${transfer.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
        <StockTransferActions transferId={transfer.id} canPost={isDraft && transfer.lines.length > 0} canVoid={isDraft} />
      </Card>

      {isDraft ? (
        <Card>
          <div className="mb-3 text-sm font-semibold">Add line</div>
          <StockTransferLineAddForm transferId={transfer.id} items={items} />
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
              {transfer.lines.map((l) => (
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
              {transfer.lines.length === 0 ? (
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

      <DocumentCollaborationPanel referenceType="TRF" referenceId={id} />
    </div>
  );
}