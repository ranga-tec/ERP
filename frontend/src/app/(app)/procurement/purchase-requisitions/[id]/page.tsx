import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { PurchaseRequisitionActions } from "../PurchaseRequisitionActions";
import { PurchaseRequisitionConvertToPoForm } from "../PurchaseRequisitionConvertToPoForm";
import { PurchaseRequisitionLineAddForm } from "../PurchaseRequisitionLineAddForm";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";

type PurchaseRequisitionDto = {
  id: string;
  number: string;
  requestDate: string;
  status: number;
  notes?: string | null;
  lines: { id: string; itemId: string; quantity: number; notes?: string | null }[];
};

type ItemDto = { id: string; sku: string; name: string };
type SupplierDto = { id: string; code: string; name: string };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Submitted",
  2: "Approved",
  3: "Rejected",
  4: "Cancelled",
};

export default async function PurchaseRequisitionDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;

  const [pr, items, suppliers] = await Promise.all([
    backendFetchJson<PurchaseRequisitionDto>(`/procurement/purchase-requisitions/${id}`),
    backendFetchJson<ItemDto[]>("/items"),
    backendFetchJson<SupplierDto[]>("/suppliers"),
  ]);

  const itemById = new Map(items.map((i) => [i.id, i]));
  const isDraft = pr.status === 0;
  const isSubmitted = pr.status === 1;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/procurement/purchase-requisitions" className="hover:underline">
            Purchase Requisitions
          </Link>{" "}
          / <span className="font-mono text-xs">{pr.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Purchase Requisition {pr.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>Status: {statusLabel[pr.status] ?? pr.status}</div>
          <div>Requested: {new Date(pr.requestDate).toLocaleString()}</div>
        </div>
        {pr.notes ? (
          <div className="mt-2 text-sm text-zinc-500">
            Notes: <span className="text-zinc-700 dark:text-zinc-300">{pr.notes}</span>
          </div>
        ) : null}
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Actions</div>
        <PurchaseRequisitionActions
          purchaseRequisitionId={pr.id}
          canSubmit={isDraft && pr.lines.length > 0}
          canApprove={isSubmitted}
          canReject={isSubmitted}
          canCancel={pr.status === 0 || pr.status === 1}
        />
      </Card>

      {pr.status === 2 ? (
        <Card>
          <div className="mb-3 text-sm font-semibold">Convert To Purchase Order</div>
          <PurchaseRequisitionConvertToPoForm purchaseRequisitionId={pr.id} suppliers={suppliers} />
        </Card>
      ) : null}

      {isDraft ? (
        <Card>
          <div className="mb-3 text-sm font-semibold">Add line</div>
          <PurchaseRequisitionLineAddForm purchaseRequisitionId={pr.id} items={items} />
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
              {pr.lines.map((line) => (
                <tr key={line.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3">
                    {itemById.get(line.itemId)?.sku ?? line.itemId}
                  </td>
                  <td className="py-2 pr-3">{line.quantity}</td>
                  <td className="py-2 pr-3 text-zinc-500">{line.notes ?? "-"}</td>
                </tr>
              ))}
              {pr.lines.length === 0 ? (
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

      <DocumentCollaborationPanel referenceType="PR" referenceId={id} />
    </div>
  );
}