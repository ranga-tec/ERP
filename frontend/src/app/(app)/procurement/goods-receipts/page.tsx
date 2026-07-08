import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { ListViewEditActions } from "@/components/ListViewEditActions";
import { SearchableRow, SearchableTable } from "@/components/SearchableTable";
import { TransactionLink } from "@/components/TransactionLink";
import { Card } from "@/components/ui";
import { GoodsReceiptCreateForm } from "./GoodsReceiptCreateForm";

type GoodsReceiptSummaryDto = {
  id: string;
  number: string;
  purchaseOrderId: string;
  warehouseId: string;
  receivedAt: string;
  status: number;
};

type PurchaseOrderSummaryDto = { id: string; number: string; status: number };
type WarehouseDto = { id: string; code: string; name: string };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Voided",
};

export default async function GoodsReceiptsPage() {
  const [grns, pos, warehouses] = await Promise.all([
    backendFetchJson<GoodsReceiptSummaryDto[]>("/procurement/goods-receipts?take=100"),
    backendFetchJson<PurchaseOrderSummaryDto[]>("/procurement/purchase-orders?take=500"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
  ]);

  const poById = new Map(pos.map((p) => [p.id, p]));
  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Goods Receipts (GRN)</h1>
        <p className="mt-1 text-sm text-zinc-500">Receive PO items into a warehouse, then post.</p>
      </div>

      <AppFormModal title="Create Goods Receipt" description="Create a draft GRN for a purchase order." buttonLabel="+ New GRN">
        <GoodsReceiptCreateForm purchaseOrders={pos} warehouses={warehouses} />
      </AppFormModal>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <SearchableTable placeholder="Search goods receipts..." emptyMessage="No GRNs yet." emptyColSpan={6} headers={
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">PO</th>
                <th className="py-2 pr-3">Warehouse</th>
                <th className="py-2 pr-3">Received</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
          }>
              {grns.map((g) => {
                const po = poById.get(g.purchaseOrderId)?.number ?? g.purchaseOrderId;
                const warehouse = warehouseById.get(g.warehouseId)?.code ?? g.warehouseId;
                const status = statusLabel[g.status] ?? String(g.status);
                return (
                <SearchableRow key={g.id} searchText={[g.number, po, warehouse, status].join(" ")}>
                <tr key={g.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/procurement/goods-receipts/${g.id}`}>
                      {g.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-600 dark:text-zinc-400">
                    <TransactionLink referenceType="PO" referenceId={g.purchaseOrderId} monospace>
                      {po}
                    </TransactionLink>
                  </td>
                  <td className="py-2 pr-3">{warehouse}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(g.receivedAt).toLocaleString()}</td>
                  <td className="py-2 pr-3">{status}</td>
                  <td className="py-2 pr-3">
                    <ListViewEditActions
                      viewHref={`/procurement/goods-receipts/${g.id}`}
                      canEdit={g.status === 0}
                      auditTableName="GoodsReceipts"
                      auditRecordId={g.id}
                    />
                  </td>
                </tr>
                </SearchableRow>
                );
              })}
        </SearchableTable>
      </Card>
    </div>
  );
}
