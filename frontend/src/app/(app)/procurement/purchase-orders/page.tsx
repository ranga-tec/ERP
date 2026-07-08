import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { ListViewEditActions } from "@/components/ListViewEditActions";
import { SearchableRow, SearchableTable } from "@/components/SearchableTable";
import { Card } from "@/components/ui";
import { PurchaseOrderCreateForm } from "./PurchaseOrderCreateForm";

type SupplierDto = { id: string; code: string; name: string };
type PurchaseOrderSummaryDto = {
  id: string;
  number: string;
  supplierId: string;
  supplierCode?: string | null;
  supplierName?: string | null;
  orderDate: string;
  status: number;
  total: number;
};

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Approved",
  2: "Partially Received",
  3: "Closed",
  4: "Cancelled",
};

export default async function PurchaseOrdersPage() {
  const [pos, suppliers] = await Promise.all([
    backendFetchJson<PurchaseOrderSummaryDto[]>("/procurement/purchase-orders?take=100"),
    backendFetchJson<SupplierDto[]>("/suppliers"),
  ]);

  const supplierById = new Map(suppliers.map((s) => [s.id, s]));
  const supplierLabel = (po: PurchaseOrderSummaryDto) => {
    if (po.supplierCode && po.supplierName) return `${po.supplierCode} - ${po.supplierName}`;
    if (po.supplierCode) return po.supplierCode;
    return supplierById.get(po.supplierId)?.code ?? po.supplierId;
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Purchase Orders</h1>
        <p className="mt-1 text-sm text-zinc-500">PO workflow: draft → approve → receive.</p>
      </div>

      <AppFormModal title="Create Purchase Order" description="Create a draft purchase order before adding lines." buttonLabel="+ New PO">
        <PurchaseOrderCreateForm suppliers={suppliers} />
      </AppFormModal>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <SearchableTable placeholder="Search purchase orders..." emptyMessage="No purchase orders yet." emptyColSpan={6} headers={
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Supplier</th>
                <th className="py-2 pr-3">Order Date</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Total</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
          }>
              {pos.map((p) => {
                const supplier = supplierLabel(p);
                const status = statusLabel[p.status] ?? String(p.status);
                return (
                <SearchableRow key={p.id} searchText={[p.number, supplier, status, p.total].join(" ")}>
                <tr key={p.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/procurement/purchase-orders/${p.id}`}>
                      {p.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">{supplier}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(p.orderDate).toLocaleString()}</td>
                  <td className="py-2 pr-3">{status}</td>
                  <td className="py-2 pr-3">{p.total}</td>
                  <td className="py-2 pr-3">
                    <ListViewEditActions
                      viewHref={`/procurement/purchase-orders/${p.id}`}
                      canEdit={p.status === 0}
                      auditTableName="PurchaseOrders"
                      auditRecordId={p.id}
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
