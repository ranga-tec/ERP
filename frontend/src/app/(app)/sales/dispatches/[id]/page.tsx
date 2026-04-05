import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { ItemInlineLink } from "@/components/InlineLink";
import { TransactionLink } from "@/components/TransactionLink";
import { Card, SecondaryLink } from "@/components/ui";
import { DispatchActions } from "../DispatchActions";
import { DispatchLineAddForm } from "../DispatchLineAddForm";
import { DispatchLinesEditor } from "../DispatchLinesEditor";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";
import { StockAvailabilityExplorer } from "@/components/StockAvailabilityExplorer";

type DispatchDto = {
  id: string;
  number: string;
  salesOrderId: string;
  warehouseId: string;
  dispatchedAt: string;
  status: number;
  lines: { id: string; itemId: string; quantity: number; batchNumber?: string | null; serials: string[] }[];
};

type SalesOrderSummaryDto = { id: string; number: string };
type WarehouseDto = { id: string; code: string; name: string };
type ItemDto = { id: string; sku: string; name: string; trackingType: number };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Voided",
};

export default async function DispatchDetailPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ mode?: string }>;
}) {
  const { id } = await params;
  const { mode } = await searchParams;
  const startInEditMode = mode === "edit";

  const [dispatch, orders, warehouses, items] = await Promise.all([
    backendFetchJson<DispatchDto>(`/sales/dispatches/${id}`),
    backendFetchJson<SalesOrderSummaryDto[]>("/sales/orders?take=500"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<ItemDto[]>("/items"),
  ]);

  const orderById = new Map(orders.map((o) => [o.id, o]));
  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));
  const isDraft = dispatch.status === 0;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/sales/dispatches" className="hover:underline">
            Dispatches
          </Link>{" "}
          / <span className="font-mono text-xs">{dispatch.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Dispatch {dispatch.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>
            Order:{" "}
            <TransactionLink referenceType="SO" referenceId={dispatch.salesOrderId} monospace>
              {orderById.get(dispatch.salesOrderId)?.number ?? dispatch.salesOrderId}
            </TransactionLink>
          </div>
          <div>Warehouse: {warehouseById.get(dispatch.warehouseId)?.code ?? dispatch.warehouseId}</div>
          <div>Status: {statusLabel[dispatch.status] ?? dispatch.status}</div>
          <div>Date: {new Date(dispatch.dispatchedAt).toLocaleString()}</div>
        </div>
      </div>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink
            href={`/api/backend/sales/dispatches/${dispatch.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
        <DispatchActions dispatchId={dispatch.id} canPost={isDraft && dispatch.lines.length > 0} />
      </Card>

      {isDraft ? (
        <>
          <Card>
          <div className="mb-3 text-sm font-semibold">Add line</div>
          <DispatchLineAddForm dispatchId={dispatch.id} items={items} warehouses={warehouses} warehouseId={dispatch.warehouseId} />
        </Card>

          <Card>
            <div className="mb-3 text-sm font-semibold">Stock visibility</div>
            <StockAvailabilityExplorer warehouses={warehouses} items={items} initialWarehouseId={dispatch.warehouseId} />
          </Card>
        </>
      ) : null}

      <Card>
        <div className="mb-3 text-sm font-semibold">Lines</div>
        <DispatchLinesEditor
          dispatchId={dispatch.id}
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

      <DocumentCollaborationPanel referenceType="DN" referenceId={id} />
    </div>
  );
}

