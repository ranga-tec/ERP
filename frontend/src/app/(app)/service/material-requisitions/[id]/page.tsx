import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, SecondaryLink, Table } from "@/components/ui";
import { MaterialRequisitionActions } from "../MaterialRequisitionActions";
import { MaterialRequisitionLineAddForm } from "../MaterialRequisitionLineAddForm";
import { MaterialRequisitionLineRow } from "../MaterialRequisitionLineRow";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";

type MaterialRequisitionDto = {
  id: string;
  number: string;
  serviceJobId: string;
  warehouseId: string;
  requestedAt: string;
  status: number;
  lines: { id: string; itemId: string; quantity: number; batchNumber?: string | null; serials: string[] }[];
};

type ServiceJobDto = { id: string; number: string };
type WarehouseDto = { id: string; code: string; name: string };
type ItemDto = { id: string; sku: string; name: string; trackingType: number };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Voided",
};

export default async function MaterialRequisitionDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [mr, jobs, warehouses, items] = await Promise.all([
    backendFetchJson<MaterialRequisitionDto>(`/service/material-requisitions/${id}`),
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=500"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<ItemDto[]>("/items"),
  ]);

  const jobById = new Map(jobs.map((j) => [j.id, j]));
  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));
  const itemById = new Map(items.map((i) => [i.id, i]));
  const isDraft = mr.status === 0;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/service/material-requisitions" className="hover:underline">
            Material Reqs
          </Link>{" "}
          / <span className="font-mono text-xs">{mr.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Requisition {mr.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>
            Job: <span className="font-mono text-xs">{jobById.get(mr.serviceJobId)?.number ?? mr.serviceJobId}</span>
          </div>
          <div>Warehouse: {warehouseById.get(mr.warehouseId)?.code ?? mr.warehouseId}</div>
          <div>Status: {statusLabel[mr.status] ?? mr.status}</div>
          <div>Requested: {new Date(mr.requestedAt).toLocaleString()}</div>
        </div>
      </div>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink
            href={`/api/backend/service/material-requisitions/${mr.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
        <MaterialRequisitionActions requisitionId={mr.id} canPost={isDraft && mr.lines.length > 0} />
      </Card>

      {isDraft ? (
        <Card>
          <div className="mb-3 text-sm font-semibold">Add line</div>
          <MaterialRequisitionLineAddForm requisitionId={mr.id} items={items} />
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
                <th className="py-2 pr-3">Batch</th>
                <th className="py-2 pr-3">Serials</th>
                {isDraft ? <th className="py-2 pr-3">Actions</th> : null}
              </tr>
            </thead>
            <tbody>
              {mr.lines.map((l) => {
                const item = itemById.get(l.itemId);
                const itemLabel = item ? `${item.sku} - ${item.name}` : l.itemId;
                return (
                  <MaterialRequisitionLineRow
                    key={l.id}
                    requisitionId={mr.id}
                    line={l}
                    itemLabel={itemLabel}
                    canEdit={isDraft}
                  />
                );
              })}
              {mr.lines.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={isDraft ? 5 : 4}>
                    No lines yet.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>

      <DocumentCollaborationPanel referenceType="MR" referenceId={id} />
    </div>
  );
}

