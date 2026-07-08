import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { ListViewEditActions } from "@/components/ListViewEditActions";
import { SearchableRow, SearchableTable } from "@/components/SearchableTable";
import { TransactionLink } from "@/components/TransactionLink";
import { Card } from "@/components/ui";
import { DirectDispatchCreateForm } from "./DirectDispatchCreateForm";

type DirectDispatchSummaryDto = {
  id: string;
  number: string;
  warehouseId: string;
  customerId?: string | null;
  serviceJobId?: string | null;
  dispatchedAt: string;
  status: number;
  reason?: string | null;
  lineCount: number;
};

type CustomerDto = { id: string; code: string; name: string };
type ServiceJobDto = { id: string; number: string; customerId: string };
type WarehouseDto = { id: string; code: string; name: string };

const statusLabel: Record<number, string> = { 0: "Draft", 1: "Posted", 2: "Voided" };

export default async function DirectDispatchesPage() {
  const [rows, customers, jobs, warehouses] = await Promise.all([
    backendFetchJson<DirectDispatchSummaryDto[]>("/sales/direct-dispatches?take=100"),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=200"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
  ]);

  const customerById = new Map(customers.map((c) => [c.id, c]));
  const jobById = new Map(jobs.map((j) => [j.id, j]));
  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Advance of Dispatch (AOD)</h1>
        <p className="mt-1 text-sm text-zinc-500">Immediate stock issue without a sales order (sales/service scenarios).</p>
      </div>

      <AppFormModal title="Create AOD" description="Create an advance dispatch without a sales order." buttonLabel="+ New AOD">
        <DirectDispatchCreateForm customers={customers} serviceJobs={jobs} warehouses={warehouses} />
      </AppFormModal>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <SearchableTable placeholder="Search AOD..." emptyMessage="No direct dispatches yet." emptyColSpan={8} headers={
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Customer</th>
                <th className="py-2 pr-3">Job Order</th>
                <th className="py-2 pr-3">Warehouse</th>
                <th className="py-2 pr-3">Date</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Lines</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
          }>
              {rows.map((r) => {
                const customer = r.customerId ? customerById.get(r.customerId)?.code ?? r.customerId : "-";
                const job = r.serviceJobId ? jobById.get(r.serviceJobId)?.number ?? r.serviceJobId : "-";
                const warehouse = warehouseById.get(r.warehouseId)?.code ?? r.warehouseId;
                const status = statusLabel[r.status] ?? String(r.status);
                return (
                <SearchableRow key={r.id} searchText={[r.number, customer, job, warehouse, status, r.reason ?? ""].join(" ")}>
                <tr key={r.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/sales/direct-dispatches/${r.id}`}>
                      {r.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">{customer}</td>
                  <td className="py-2 pr-3 font-mono text-xs">
                    {r.serviceJobId ? (
                      <TransactionLink referenceType="SJ" referenceId={r.serviceJobId} monospace>
                        {jobById.get(r.serviceJobId)?.number ?? r.serviceJobId}
                      </TransactionLink>
                    ) : (
                      "-"
                    )}
                  </td>
                  <td className="py-2 pr-3">{warehouse}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(r.dispatchedAt).toLocaleString()}</td>
                  <td className="py-2 pr-3">{status}</td>
                  <td className="py-2 pr-3">{r.lineCount}</td>
                  <td className="py-2 pr-3">
                    <ListViewEditActions
                      viewHref={`/sales/direct-dispatches/${r.id}`}
                      canEdit={r.status === 0}
                      auditTableName="DirectDispatches"
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
