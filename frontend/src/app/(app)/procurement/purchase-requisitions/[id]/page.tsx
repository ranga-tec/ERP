import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { ItemInlineLink } from "@/components/InlineLink";
import { Card } from "@/components/ui";
import { PurchaseRequisitionActions } from "../PurchaseRequisitionActions";
import { PurchaseRequisitionConvertToPoForm } from "../PurchaseRequisitionConvertToPoForm";
import { PurchaseRequisitionLineAddForm } from "../PurchaseRequisitionLineAddForm";
import { PurchaseRequisitionLinesEditor } from "../PurchaseRequisitionLinesEditor";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";

type PurchaseRequisitionDto = {
  id: string;
  number: string;
  requestDate: string;
  status: number;
  notes?: string | null;
  lines: { id: string; itemId: string; quantity: number; notes?: string | null }[];
};

type ItemDto = { id: string; sku: string; name: string; unitOfMeasure: string };
type SupplierDto = { id: string; code: string; name: string };
type UomDto = { id: string; code: string; name: string; isActive: boolean };
type UnitConversionDto = {
  id: string;
  fromUnitOfMeasureCode: string;
  toUnitOfMeasureCode: string;
  factor: number;
  isActive: boolean;
};

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

  const [pr, items, suppliers, uoms, conversions] = await Promise.all([
    backendFetchJson<PurchaseRequisitionDto>(`/procurement/purchase-requisitions/${id}`),
    backendFetchJson<ItemDto[]>("/items"),
    backendFetchJson<SupplierDto[]>("/suppliers"),
    backendFetchJson<UomDto[]>("/uoms"),
    backendFetchJson<UnitConversionDto[]>("/uom-conversions"),
  ]);

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
          <PurchaseRequisitionLineAddForm
            purchaseRequisitionId={pr.id}
            items={items}
            uoms={uoms}
            conversions={conversions}
          />
        </Card>
      ) : null}

      <Card>
        <div className="mb-3 text-sm font-semibold">Lines</div>
        <PurchaseRequisitionLinesEditor
          purchaseRequisitionId={pr.id}
          lines={pr.lines}
          itemLabelById={new Map(
            items.map((item) => [
              item.id,
              <ItemInlineLink key={item.id} itemId={item.id}>
                {`${item.sku} - ${item.name}`}
              </ItemInlineLink>,
            ]),
          )}
          itemSearchLabelById={new Map(items.map((item) => [item.id, `${item.sku} ${item.name}`.toLowerCase()]))}
          baseUomByItemId={new Map(items.map((item) => [item.id, item.unitOfMeasure]))}
          canEdit={isDraft}
        />
      </Card>

      <DocumentCollaborationPanel referenceType="PR" referenceId={id} />
    </div>
  );
}

