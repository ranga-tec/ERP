import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, SecondaryLink, Table } from "@/components/ui";
import { DirectDispatchActions } from "../DirectDispatchActions";
import { DirectDispatchLineAddForm } from "../DirectDispatchLineAddForm";
import { DirectDispatchLineRow } from "../DirectDispatchLineRow";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";

type DirectDispatchDto = {
  id: string;
  number: string;
  warehouseId: string;
  customerId?: string | null;
  serviceJobId?: string | null;
  dispatchedAt: string;
  status: number;
  reason?: string | null;
  lines: { id: string; itemId: string; quantity: number; batchNumber?: string | null; serials: string[] }[];
};

type CustomerDto = { id: string; code: string; name: string };
type ServiceJobDto = { id: string; number: string; customerId: string };
type WarehouseDto = { id: string; code: string; name: string };
type ItemDto = { id: string; sku: string; name: string; trackingType: number };

const statusLabel: Record<number, string> = { 0: "Draft", 1: "Posted", 2: "Voided" };

export default async function DirectDispatchDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [dispatch, customers, jobs, warehouses, items] = await Promise.all([
    backendFetchJson<DirectDispatchDto>(`/sales/direct-dispatches/${id}`),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=200"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<ItemDto[]>("/items"),
  ]);

  const customerById = new Map(customers.map((c) => [c.id, c]));
  const jobById = new Map(jobs.map((j) => [j.id, j]));
  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));
  const itemById = new Map(items.map((i) => [i.id, i]));
  const isDraft = dispatch.status === 0;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/sales/direct-dispatches" className="hover:underline">
            Direct Dispatches
          </Link>{" "}
          / <span className="font-mono text-xs">{dispatch.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Direct Dispatch {dispatch.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>Customer: {dispatch.customerId ? customerById.get(dispatch.customerId)?.code ?? dispatch.customerId : "-"}</div>
          <div>Service Job: {dispatch.serviceJobId ? jobById.get(dispatch.serviceJobId)?.number ?? dispatch.serviceJobId : "-"}</div>
          <div>Warehouse: {warehouseById.get(dispatch.warehouseId)?.code ?? dispatch.warehouseId}</div>
          <div>Status: {statusLabel[dispatch.status] ?? dispatch.status}</div>
          <div>Date: {new Date(dispatch.dispatchedAt).toLocaleString()}</div>
        </div>
        {dispatch.reason ? <div className="mt-2 text-sm text-zinc-500">Reason: {dispatch.reason}</div> : null}
      </div>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink
            href={`/api/backend/sales/direct-dispatches/${dispatch.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
        <DirectDispatchActions directDispatchId={dispatch.id} canPost={isDraft && dispatch.lines.length > 0} />
      </Card>

      {isDraft ? (
        <Card>
          <div className="mb-3 text-sm font-semibold">Add line</div>
          <DirectDispatchLineAddForm directDispatchId={dispatch.id} items={items} />
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
              {dispatch.lines.map((l) => {
                const item = itemById.get(l.itemId);
                const itemLabel = item ? `${item.sku} - ${item.name}` : l.itemId;
                return (
                  <DirectDispatchLineRow
                    key={l.id}
                    directDispatchId={dispatch.id}
                    line={l}
                    itemLabel={itemLabel}
                    canEdit={isDraft}
                  />
                );
              })}
              {dispatch.lines.length === 0 ? (
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

      <DocumentCollaborationPanel referenceType="DDN" referenceId={id} />
    </div>
  );
}

