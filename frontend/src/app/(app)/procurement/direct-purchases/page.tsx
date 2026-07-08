import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { ListViewEditActions } from "@/components/ListViewEditActions";
import { SearchableRow, SearchableTable } from "@/components/SearchableTable";
import { TransactionLink } from "@/components/TransactionLink";
import { Card } from "@/components/ui";
import { DirectPurchaseCreateForm } from "./DirectPurchaseCreateForm";

type DirectPurchaseSummaryDto = {
  id: string;
  number: string;
  supplierId: string;
  warehouseId: string;
  serviceJobId?: string | null;
  purchasedAt: string;
  status: number;
  remarks?: string | null;
  subtotal: number;
  taxTotal: number;
  grandTotal: number;
};

type SupplierDto = { id: string; code: string; name: string };
type WarehouseDto = { id: string; code: string; name: string };
type ServiceJobDto = { id: string; number: string; kind: number };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Voided",
};

export default async function DirectPurchasesPage() {
  const [rows, suppliers, warehouses, serviceJobs] = await Promise.all([
    backendFetchJson<DirectPurchaseSummaryDto[]>("/procurement/direct-purchases?take=100"),
    backendFetchJson<SupplierDto[]>("/suppliers"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=500"),
  ]);

  const supplierById = new Map(suppliers.map((s) => [s.id, s]));
  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));
  const jobById = new Map(serviceJobs.map((job) => [job.id, job]));

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Direct Purchases</h1>
          <p className="mt-1 text-sm text-zinc-500">
            Receive purchases without a PO. Link emergency outside buys to the relevant job order when needed.
          </p>
        </div>
        <AppFormModal title="Create Direct Purchase" description="Create a draft direct purchase without a PO." buttonLabel="+ New Direct Purchase" size="xl">
          <DirectPurchaseCreateForm suppliers={suppliers} warehouses={warehouses} serviceJobs={serviceJobs} />
        </AppFormModal>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <SearchableTable placeholder="Search direct purchases..." emptyMessage="No direct purchases yet." emptyColSpan={9} headers={
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Supplier</th>
                <th className="py-2 pr-3">Warehouse</th>
                <th className="py-2 pr-3">Job Order</th>
                <th className="py-2 pr-3">Date</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Total</th>
                <th className="py-2 pr-3">Remarks</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
          }>
              {rows.map((r) => {
                const supplier = supplierById.get(r.supplierId)?.code ?? r.supplierId;
                const warehouse = warehouseById.get(r.warehouseId)?.code ?? r.warehouseId;
                const job = r.serviceJobId ? jobById.get(r.serviceJobId)?.number ?? r.serviceJobId : "-";
                const status = statusLabel[r.status] ?? String(r.status);
                return (
                <SearchableRow key={r.id} searchText={[r.number, supplier, warehouse, job, status, r.remarks ?? "", r.grandTotal].join(" ")}>
                <tr key={r.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/procurement/direct-purchases/${r.id}`}>
                      {r.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">{supplier}</td>
                  <td className="py-2 pr-3">{warehouse}</td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-600 dark:text-zinc-400">
                    {r.serviceJobId ? (
                      <TransactionLink referenceType="SJ" referenceId={r.serviceJobId} monospace>
                        {job}
                      </TransactionLink>
                    ) : (
                      "-"
                    )}
                  </td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(r.purchasedAt).toLocaleString()}</td>
                  <td className="py-2 pr-3">{status}</td>
                  <td className="py-2 pr-3">{r.grandTotal.toFixed(2)}</td>
                  <td className="py-2 pr-3 text-zinc-500">{r.remarks ?? "-"}</td>
                  <td className="py-2 pr-3">
                    <ListViewEditActions
                      viewHref={`/procurement/direct-purchases/${r.id}`}
                      canEdit={r.status === 0}
                      editInModal
                      editModalTitle={`Edit Direct Purchase ${r.number}`}
                      auditTableName="DirectPurchases"
                      auditRecordId={r.id}
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
