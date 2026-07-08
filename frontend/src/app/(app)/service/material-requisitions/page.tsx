import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { SearchableRow, SearchableTable } from "@/components/SearchableTable";
import { TransactionLink } from "@/components/TransactionLink";
import { Card } from "@/components/ui";
import { MaterialRequisitionCreateForm } from "./MaterialRequisitionCreateForm";
import { MaterialRequisitionEditForm } from "./MaterialRequisitionEditForm";
import { AuditTrailButton } from "@/components/AuditTrailButton";

type MaterialRequisitionSummaryDto = {
  id: string;
  number: string;
  serviceJobId: string;
  warehouseId: string;
  requestedAt: string;
  status: number;
  lineCount: number;
};

type ServiceJobDto = { id: string; number: string };
type WarehouseDto = { id: string; code: string; name: string };
type CurrentUserPermissionsDto = { userId: string; permissions: string[] };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Voided",
};

export default async function MaterialRequisitionsPage() {
  const [mrs, jobs, warehouses, currentUserPermissions] = await Promise.all([
    backendFetchJson<MaterialRequisitionSummaryDto[]>("/service/material-requisitions?take=100"),
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=500"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<CurrentUserPermissionsDto>("/me/permissions"),
  ]);

  const permissions = new Set(currentUserPermissions.permissions);
  const canCreate = permissions.has("Service.MaterialRequisition.Create");
  const canEdit = permissions.has("Service.MaterialRequisition.Edit");
  const jobById = new Map(jobs.map((j) => [j.id, j]));
  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Material Request Note (MRN)</h1>
          <p className="mt-1 text-sm text-zinc-500">Request and issue materials to a job order from a warehouse.</p>
        </div>
        {canCreate ? (
          <AppFormModal title="Create MRN" description="Create a draft material request note under a job order." buttonLabel="+ New MRN">
            <MaterialRequisitionCreateForm serviceJobs={jobs} warehouses={warehouses} />
          </AppFormModal>
        ) : null}
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <SearchableTable
          placeholder="Search MRN, job, warehouse, status..."
          emptyMessage="No material requisitions yet."
          emptyColSpan={7}
          headers={
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Job</th>
                <th className="py-2 pr-3">Warehouse</th>
                <th className="py-2 pr-3">Requested</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Lines</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
          }
        >
              {mrs.map((m) => {
                const job = jobById.get(m.serviceJobId);
                const warehouse = warehouseById.get(m.warehouseId);
                const status = statusLabel[m.status] ?? String(m.status);
                return (
                <SearchableRow
                  key={m.id}
                  searchText={[m.number, job?.number, warehouse?.code, warehouse?.name, status, String(m.lineCount)].filter(Boolean).join(" ")}
                >
                <tr key={m.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/service/material-requisitions/${m.id}`}>
                      {m.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-600 dark:text-zinc-400">
                    <TransactionLink referenceType="SJ" referenceId={m.serviceJobId} monospace>
                      {jobById.get(m.serviceJobId)?.number ?? m.serviceJobId}
                    </TransactionLink>
                  </td>
                  <td className="py-2 pr-3">{warehouseById.get(m.warehouseId)?.code ?? m.warehouseId}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(m.requestedAt).toLocaleString()}</td>
                  <td className="py-2 pr-3">{statusLabel[m.status] ?? m.status}</td>
                  <td className="py-2 pr-3">{m.lineCount}</td>
                  <td className="py-2 pr-3">
                    <div className="flex flex-wrap items-center gap-3 text-xs">
                      <Link className="font-semibold text-[var(--link)] underline underline-offset-2" href={`/service/material-requisitions/${m.id}`}>
                        View
                      </Link>
                      {m.status === 0 && canEdit ? (
                        <AppFormModal title={`Edit MRN ${m.number}`} description="Update draft MRN job, warehouse, or purpose." buttonLabel="Edit" variant="secondary">
                          <MaterialRequisitionEditForm requisition={m} serviceJobs={jobs} warehouses={warehouses} />
                        </AppFormModal>
                      ) : (
                        <span className="text-zinc-400">Edit</span>
                      )}
                      <AuditTrailButton tableName="MaterialRequisitions" recordId={m.id} />
                    </div>
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
