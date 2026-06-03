import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { AuditTrailButton } from "@/components/AuditTrailButton";
import { TransactionLink } from "@/components/TransactionLink";
import { Card, Table } from "@/components/ui";
import { ServiceJobCreateForm } from "./ServiceJobCreateForm";
import { ServiceJobEditForm } from "./ServiceJobEditForm";

type ServiceJobDto = {
  id: string;
  number: string;
  equipmentUnitId: string;
  customerId: string;
  openedAt: string;
  problemDescription: string;
  kind: number;
  status: number;
  estimatedStartAt?: string | null;
  actualStartAt?: string | null;
  completedAt?: string | null;
  expectedCompletionAt?: string | null;
  siteLocation?: string | null;
  jobDescription?: string | null;
  customerComplaint?: string | null;
  internalRemarks?: string | null;
  responsibleOfficerName?: string | null;
  serviceContractId?: string | null;
  serviceContractNumber?: string | null;
  entitlementSource: number;
  entitlementCoverage: number;
  customerBillingTreatment: number;
  entitlementEvaluatedAt?: string | null;
  entitlementSummary?: string | null;
};

type EquipmentUnitDto = { id: string; serialNumber: string; itemId: string; customerId: string };
type CustomerDto = { id: string; code: string; name: string };
type ItemDto = { id: string; sku: string; name: string };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Open",
  2: "Assigned",
  3: "In Progress",
  4: "Waiting for Parts",
  5: "Waiting for Customer Approval",
  6: "Waiting for Supplier",
  7: "Work Completed",
  8: "Pending Expense Settlement",
  9: "Pending Material Return",
  10: "Ready for Invoice",
  11: "Invoiced",
  12: "Closed",
  13: "Reopened",
  14: "Cancelled",
};

const kindLabel: Record<number, string> = {
  0: "Service",
  1: "Repair",
  2: "PDI",
  3: "Warranty",
  4: "Inspection",
};

const entitlementSourceLabel: Record<number, string> = {
  0: "None",
  1: "Warranty",
  2: "Contract",
};

const billingTreatmentLabel: Record<number, string> = {
  0: "Billable",
  1: "Partially Covered",
  2: "Covered No Charge",
};

export default async function ServiceJobsPage() {
  const [jobs, units, customers, items] = await Promise.all([
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=100"),
    backendFetchJson<EquipmentUnitDto[]>("/service/equipment-units?take=2000"),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<ItemDto[]>("/items/options"),
  ]);

  const unitById = new Map(units.map((u) => [u.id, u]));
  const customerById = new Map(customers.map((c) => [c.id, c]));
  const itemById = new Map(items.map((item) => [item.id, item]));
  const equipmentUnitOptions = units.map((unit) => {
    const item = itemById.get(unit.itemId);
    const customer = customerById.get(unit.customerId);
    return {
      ...unit,
      itemSku: item?.sku,
      itemName: item?.name,
      customerCode: customer?.code,
    };
  });

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Job Orders</h1>
          <p className="mt-1 text-sm text-zinc-500">Open, assign, execute, complete, invoice, close, and reopen when authorized.</p>
        </div>
        <AppFormModal title="Create New Job Order" description="Open a service, repair, PDI, warranty, or inspection job." buttonLabel="+ New Job Order">
          <ServiceJobCreateForm equipmentUnits={equipmentUnitOptions} customers={customers} />
        </AppFormModal>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Job Orders</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Equipment</th>
                <th className="py-2 pr-3">Customer</th>
                <th className="py-2 pr-3">Type</th>
                <th className="py-2 pr-3">Entitlement</th>
                <th className="py-2 pr-3">Billing</th>
                <th className="py-2 pr-3">Opened</th>
                <th className="py-2 pr-3">Responsible</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Completed</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {jobs.map((j) => (
                <tr key={j.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/service/jobs/${j.id}`}>
                      {j.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-600 dark:text-zinc-400">
                    <TransactionLink referenceType="EUNIT" referenceId={j.equipmentUnitId} monospace>
                      {unitById.get(j.equipmentUnitId)?.serialNumber ?? j.equipmentUnitId}
                    </TransactionLink>
                  </td>
                  <td className="py-2 pr-3">{customerById.get(j.customerId)?.code ?? j.customerId}</td>
                  <td className="py-2 pr-3">{kindLabel[j.kind] ?? j.kind}</td>
                  <td className="py-2 pr-3">
                    {j.serviceContractId && j.serviceContractNumber ? (
                      <Link className="hover:underline" href={`/service/contracts/${j.serviceContractId}`}>
                        {j.serviceContractNumber}
                      </Link>
                    ) : (
                      entitlementSourceLabel[j.entitlementSource] ?? j.entitlementSource
                    )}
                  </td>
                  <td className="py-2 pr-3">{billingTreatmentLabel[j.customerBillingTreatment] ?? j.customerBillingTreatment}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(j.openedAt).toLocaleString()}</td>
                  <td className="py-2 pr-3">{j.responsibleOfficerName ?? "-"}</td>
                  <td className="py-2 pr-3">{statusLabel[j.status] ?? j.status}</td>
                  <td className="py-2 pr-3 text-zinc-500">
                    {j.completedAt ? new Date(j.completedAt).toLocaleString() : "-"}
                  </td>
                  <td className="py-2 pr-3">
                    <div className="flex flex-wrap items-center gap-3 text-xs">
                      <Link className="font-semibold text-[var(--link)] underline underline-offset-2" href={`/service/jobs/${j.id}`}>
                        View
                      </Link>
                      {j.status === 0 || j.status === 1 || j.status === 13 ? (
                        <AppFormModal
                          title={`Edit Job Order ${j.number}`}
                          description="Update equipment, customer, job type, dates, site, responsibility, and intake notes."
                          buttonLabel="Edit"
                          variant="secondary"
                        >
                          <ServiceJobEditForm job={j} equipmentUnits={equipmentUnitOptions} customers={customers} />
                        </AppFormModal>
                      ) : (
                        <span className="text-zinc-400">Edit</span>
                      )}
                      <AuditTrailButton tableName="ServiceJobs" recordId={j.id} />
                    </div>
                  </td>
                </tr>
              ))}
              {jobs.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={11}>
                    No job orders yet.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>
    </div>
  );
}
