import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { ItemInlineLink } from "@/components/InlineLink";
import { TransactionLink } from "@/components/TransactionLink";
import { Card, SecondaryLink } from "@/components/ui";
import { DirectDispatchActions } from "../DirectDispatchActions";
import { DirectDispatchLineAddForm } from "../DirectDispatchLineAddForm";
import { DirectDispatchLinesEditor } from "../DirectDispatchLinesEditor";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";
import { StockAvailabilityExplorer } from "@/components/StockAvailabilityExplorer";

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

export default async function DirectDispatchDetailPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ mode?: string }>;
}) {
  const { id } = await params;
  const { mode } = await searchParams;
  const startInEditMode = mode === "edit";

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
          <div>
            Service Job:{" "}
            {dispatch.serviceJobId ? (
              <TransactionLink referenceType="SJ" referenceId={dispatch.serviceJobId}>
                {jobById.get(dispatch.serviceJobId)?.number ?? dispatch.serviceJobId}
              </TransactionLink>
            ) : (
              "-"
            )}
          </div>
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
        <>
          <Card>
          <div className="mb-3 text-sm font-semibold">Add line</div>
          <DirectDispatchLineAddForm
            directDispatchId={dispatch.id}
            items={items}
            warehouses={warehouses}
            warehouseId={dispatch.warehouseId}
          />
        </Card>

          <Card>
            <div className="mb-3 text-sm font-semibold">Stock visibility</div>
            <StockAvailabilityExplorer warehouses={warehouses} items={items} initialWarehouseId={dispatch.warehouseId} />
          </Card>
        </>
      ) : null}

      <Card>
        <div className="mb-3 text-sm font-semibold">Lines</div>
        <DirectDispatchLinesEditor
          directDispatchId={dispatch.id}
          warehouseId={dispatch.warehouseId}
          warehouses={warehouses}
          lines={dispatch.lines}
          itemLabelById={new Map(
            items.map((item) => [
              item.id,
              <ItemInlineLink key={item.id} itemId={item.id}>
                {`${item.sku} - ${item.name}`}
              </ItemInlineLink>,
            ]),
          )}
          itemSearchLabelById={new Map(items.map((item) => [item.id, `${item.sku} ${item.name}`.toLowerCase()]))}
          startInEditMode={startInEditMode}
          canEdit={isDraft}
        />
      </Card>

      <DocumentCollaborationPanel referenceType="DDN" referenceId={id} />
    </div>
  );
}

