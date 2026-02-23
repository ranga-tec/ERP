import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, SecondaryLink, Table } from "@/components/ui";
import { RfqActions } from "../RfqActions";
import { RfqLineAddForm } from "../RfqLineAddForm";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";

type RfqDto = {
  id: string;
  number: string;
  supplierId: string;
  requestedAt: string;
  status: number;
  lines: { id: string; itemId: string; quantity: number; notes?: string | null }[];
};

type SupplierDto = { id: string; code: string; name: string };
type ItemDto = { id: string; sku: string; name: string };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Sent",
  2: "Closed",
  3: "Cancelled",
};

export default async function RfqDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [rfq, suppliers, items] = await Promise.all([
    backendFetchJson<RfqDto>(`/procurement/rfqs/${id}`),
    backendFetchJson<SupplierDto[]>("/suppliers"),
    backendFetchJson<ItemDto[]>("/items"),
  ]);

  const supplierById = new Map(suppliers.map((s) => [s.id, s]));
  const itemById = new Map(items.map((i) => [i.id, i]));
  const isDraft = rfq.status === 0;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/procurement/rfqs" className="hover:underline">
            RFQs
          </Link>{" "}
          / <span className="font-mono text-xs">{rfq.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">RFQ {rfq.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>
            Supplier:{" "}
            <span className="font-medium text-zinc-900 dark:text-zinc-100">
              {supplierById.get(rfq.supplierId)?.code ?? rfq.supplierId}
            </span>
          </div>
          <div>Status: {statusLabel[rfq.status] ?? rfq.status}</div>
          <div>Requested: {new Date(rfq.requestedAt).toLocaleString()}</div>
        </div>
      </div>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink
            href={`/api/backend/procurement/rfqs/${rfq.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
        <RfqActions rfqId={rfq.id} canSend={isDraft && rfq.lines.length > 0} />
      </Card>

      {isDraft ? (
        <Card>
          <div className="mb-3 text-sm font-semibold">Add line</div>
          <RfqLineAddForm rfqId={rfq.id} items={items} />
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
                <th className="py-2 pr-3">Notes</th>
              </tr>
            </thead>
            <tbody>
              {rfq.lines.map((l) => (
                <tr key={l.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3">
                    {itemById.get(l.itemId)?.sku ?? l.itemId}
                  </td>
                  <td className="py-2 pr-3">{l.quantity}</td>
                  <td className="py-2 pr-3 text-zinc-500">{l.notes ?? "—"}</td>
                </tr>
              ))}
              {rfq.lines.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={3}>
                    No lines yet.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>

      <DocumentCollaborationPanel referenceType="RFQ" referenceId={id} />
    </div>
  );
}