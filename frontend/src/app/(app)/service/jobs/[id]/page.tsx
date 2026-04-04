import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { ItemInlineLink } from "@/components/InlineLink";
import { Card, SecondaryLink, Table } from "@/components/ui";
import { ServiceJobActions } from "../ServiceJobActions";
import { ServiceJobEditForm } from "../ServiceJobEditForm";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";
import { TransactionLink } from "@/components/TransactionLink";

type ServiceJobDto = {
  id: string;
  number: string;
  equipmentUnitId: string;
  customerId: string;
  openedAt: string;
  problemDescription: string;
  kind: number;
  status: number;
  completedAt?: string | null;
  serviceContractId?: string | null;
  serviceContractNumber?: string | null;
  entitlementSource: number;
  entitlementCoverage: number;
  customerBillingTreatment: number;
  entitlementEvaluatedAt?: string | null;
  entitlementSummary?: string | null;
};

type EquipmentUnitDto = { id: string; serialNumber: string; customerId: string };
type CustomerDto = { id: string; code: string; name: string };
type ServiceJobCostingDto = {
  serviceJobId: string;
  jobNumber: string;
  latestApprovedEstimateTotal?: number | null;
  latestDraftEstimateTotal?: number | null;
  draftInvoiceTotal: number;
  postedInvoiceTotal: number;
  materialConsumedCost: number;
  directPurchaseCost: number;
  approvedLaborCost: number;
  pendingLaborCost: number;
  approvedExpenseClaimCost: number;
  pendingExpenseClaimCost: number;
  billableLaborRevenue: number;
  uninvoicedBillableLaborRevenue: number;
  billableExpenseClaimCost: number;
  unconvertedBillableExpenseClaimCost: number;
  totalActualCost: number;
  quotedGrossMargin?: number | null;
  postedGrossMargin: number;
  estimates: { id: string; number: string; revisionNumber: number; status: number; total: number }[];
  invoices: { id: string; number: string; status: number; total: number }[];
  materialLines: {
    materialRequisitionId: string;
    materialRequisitionNumber: string;
    itemId: string;
    itemSku: string;
    itemName: string;
    quantity: number;
    unitCost: number;
    lineTotal: number;
  }[];
  directPurchaseLines: {
    directPurchaseId: string;
    directPurchaseNumber: string;
    supplierCode: string;
    itemId: string;
    itemSku: string;
    itemName: string;
    quantity: number;
    unitPrice: number;
    taxPercent: number;
    lineTotal: number;
  }[];
  laborLines: {
    workDate: string;
    workOrderId: string;
    timeEntryId: string;
    technicianName: string;
    workDescription: string;
    status: number;
    hoursWorked: number;
    costRate: number;
    laborCost: number;
    billableToCustomer: boolean;
    billableHours: number;
    billingRate: number;
    taxPercent: number;
    billableTotal: number;
    effectiveBillableTotal: number;
    salesInvoiceId?: string | null;
    salesInvoiceLineId?: string | null;
  }[];
  expenseClaimLines: {
    expenseClaimId: string;
    expenseClaimNumber: string;
    status: number;
    itemId?: string | null;
    itemSku?: string | null;
    itemName?: string | null;
    description: string;
    quantity: number;
    unitCost: number;
    billableToCustomer: boolean;
    convertedToServiceEstimateId?: string | null;
    lineTotal: number;
  }[];
};

const statusLabel: Record<number, string> = {
  0: "Open",
  1: "In Progress",
  2: "Completed",
  3: "Closed",
  4: "Cancelled",
};

const kindLabel: Record<number, string> = {
  0: "Service",
  1: "Repair",
};

const entitlementSourceLabel: Record<number, string> = {
  0: "None",
  1: "Manufacturer Warranty",
  2: "Service Contract",
};

const entitlementCoverageLabel: Record<number, string> = {
  0: "No Warranty",
  1: "Inspection Only",
  2: "Labor Only",
  3: "Parts Only",
  4: "Labor and Parts",
};

const billingTreatmentLabel: Record<number, string> = {
  0: "Billable",
  1: "Partially Covered",
  2: "Covered No Charge",
};

const estimateStatusLabel: Record<number, string> = {
  0: "Draft",
  1: "Approved",
  2: "Rejected",
};

const invoiceStatusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Paid",
  3: "Voided",
};

const claimStatusLabel: Record<number, string> = {
  0: "Draft",
  1: "Submitted",
  2: "Approved",
  3: "Rejected",
  4: "Settled",
};

const laborStatusLabel: Record<number, string> = {
  0: "Draft",
  1: "Submitted",
  2: "Approved",
  3: "Rejected",
  4: "Invoiced",
};

function money(value?: number | null) {
  return typeof value === "number" ? value.toFixed(2) : "-";
}

export default async function ServiceJobDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [job, units, customers, costing] = await Promise.all([
    backendFetchJson<ServiceJobDto>(`/service/jobs/${id}`),
    backendFetchJson<EquipmentUnitDto[]>("/service/equipment-units?take=2000"),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<ServiceJobCostingDto>(`/service/jobs/${id}/costing`),
  ]);

  const selectedUnit =
    units.some((unit) => unit.id === job.equipmentUnitId)
      ? null
      : await backendFetchJson<EquipmentUnitDto>(`/service/equipment-units/${job.equipmentUnitId}`).catch(() => null);
  const availableUnits = selectedUnit ? [selectedUnit, ...units] : units;

  const unitById = new Map(availableUnits.map((u) => [u.id, u]));
  const customerById = new Map(customers.map((c) => [c.id, c]));

  const canStart = job.status === 0;
  const canComplete = job.status === 0 || job.status === 1;
  const canClose = job.status === 2;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/service/jobs" className="hover:underline">
            Jobs
          </Link>{" "}
          / <span className="font-mono text-xs">{job.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Job {job.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>
            Equipment:{" "}
            <TransactionLink referenceType="EUNIT" referenceId={job.equipmentUnitId} monospace>
              {unitById.get(job.equipmentUnitId)?.serialNumber ?? job.equipmentUnitId}
            </TransactionLink>
          </div>
          <div>Customer: {customerById.get(job.customerId)?.code ?? job.customerId}</div>
          <div>Type: {kindLabel[job.kind] ?? job.kind}</div>
          <div>Status: {statusLabel[job.status] ?? job.status}</div>
          <div>Opened: {new Date(job.openedAt).toLocaleString()}</div>
          <div>Completed: {job.completedAt ? new Date(job.completedAt).toLocaleString() : "-"}</div>
        </div>
      </div>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink
            href={`/api/backend/service/jobs/${job.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
        <ServiceJobActions jobId={job.id} canStart={canStart} canComplete={canComplete} canClose={canClose} />
      </Card>

      {job.status === 0 ? (
        <Card>
          <div className="mb-3 text-sm font-semibold">Edit Job</div>
          <ServiceJobEditForm job={job} equipmentUnits={availableUnits} customers={customers} />
        </Card>
      ) : (
        <Card>
          <div className="text-sm text-zinc-500">
            Job header fields are editable while the job is still open. After work starts, use the job status flow and linked service documents instead of changing the intake header.
          </div>
        </Card>
      )}

      <Card>
        <div className="mb-2 text-sm font-semibold">Problem</div>
        <div className="whitespace-pre-wrap text-sm text-zinc-700 dark:text-zinc-200">{job.problemDescription}</div>
      </Card>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Entitlement Source</div>
          <div className="mt-2 text-sm font-medium">
            {job.serviceContractId && job.serviceContractNumber ? (
              <Link className="hover:underline" href={`/service/contracts/${job.serviceContractId}`}>
                {job.serviceContractNumber}
              </Link>
            ) : (
              entitlementSourceLabel[job.entitlementSource] ?? job.entitlementSource
            )}
          </div>
          <div className="mt-1 text-xs text-zinc-500">
            {job.entitlementEvaluatedAt ? `Evaluated ${new Date(job.entitlementEvaluatedAt).toLocaleString()}` : "Not evaluated yet"}
          </div>
        </Card>
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Coverage</div>
          <div className="mt-2 text-sm font-medium">
            {entitlementCoverageLabel[job.entitlementCoverage] ?? job.entitlementCoverage}
          </div>
        </Card>
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Billing Treatment</div>
          <div className="mt-2 text-sm font-medium">
            {billingTreatmentLabel[job.customerBillingTreatment] ?? job.customerBillingTreatment}
          </div>
        </Card>
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Contract Link</div>
          <div className="mt-2 text-sm font-medium">
            {job.serviceContractId && job.serviceContractNumber ? (
              <Link className="hover:underline" href={`/service/contracts/${job.serviceContractId}`}>
                {job.serviceContractNumber}
              </Link>
            ) : (
              "No linked contract"
            )}
          </div>
        </Card>
      </div>

      {job.entitlementSummary ? (
        <Card>
          <div className="mb-2 text-sm font-semibold">Entitlement Summary</div>
          <div className="text-sm text-zinc-700 dark:text-zinc-200">{job.entitlementSummary}</div>
        </Card>
      ) : null}

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Actual Cost</div>
          <div className="mt-2 text-2xl font-semibold">{money(costing.totalActualCost)}</div>
          <div className="mt-1 text-xs text-zinc-500">Materials + direct purchase + approved labor + approved claims</div>
        </Card>
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Quoted Revenue</div>
          <div className="mt-2 text-2xl font-semibold">
            {money(costing.latestApprovedEstimateTotal ?? costing.latestDraftEstimateTotal)}
          </div>
          <div className="mt-1 text-xs text-zinc-500">Approved estimate preferred, otherwise latest draft</div>
        </Card>
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Posted Invoice Revenue</div>
          <div className="mt-2 text-2xl font-semibold">{money(costing.postedInvoiceTotal)}</div>
          <div className="mt-1 text-xs text-zinc-500">Draft invoices: {money(costing.draftInvoiceTotal)}</div>
        </Card>
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Uninvoiced Billable Labor</div>
          <div className="mt-2 text-2xl font-semibold">{money(costing.uninvoicedBillableLaborRevenue)}</div>
          <div className="mt-1 text-xs text-zinc-500">Approved timesheets ready for customer billing after entitlement coverage</div>
        </Card>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Job Costing</div>
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3 text-sm">
          <div className="rounded-xl border border-[var(--card-border)] p-3">
            <div className="font-medium">Cost Breakdown</div>
            <div className="mt-2 text-zinc-500">Material consumption: {money(costing.materialConsumedCost)}</div>
            <div className="text-zinc-500">Direct purchases: {money(costing.directPurchaseCost)}</div>
            <div className="text-zinc-500">Approved labor: {money(costing.approvedLaborCost)}</div>
            <div className="text-zinc-500">Pending labor: {money(costing.pendingLaborCost)}</div>
            <div className="text-zinc-500">Approved expense claims: {money(costing.approvedExpenseClaimCost)}</div>
            <div className="text-zinc-500">Pending expense claims: {money(costing.pendingExpenseClaimCost)}</div>
          </div>
          <div className="rounded-xl border border-[var(--card-border)] p-3">
            <div className="font-medium">Margin View</div>
            <div className="mt-2 text-zinc-500">Quoted gross margin: {money(costing.quotedGrossMargin)}</div>
            <div className="text-zinc-500">Posted gross margin: {money(costing.postedGrossMargin)}</div>
            <div className="text-zinc-500">Billable labor revenue: {money(costing.billableLaborRevenue)}</div>
            <div className="text-zinc-500">Uninvoiced billable labor: {money(costing.uninvoicedBillableLaborRevenue)}</div>
            <div className="text-zinc-500">Billable claim cost: {money(costing.billableExpenseClaimCost)}</div>
            <div className="text-zinc-500">Unconverted billable claims: {money(costing.unconvertedBillableExpenseClaimCost)}</div>
          </div>
          <div className="rounded-xl border border-[var(--card-border)] p-3">
            <div className="font-medium">Estimate / Invoice Trail</div>
            <div className="mt-2 text-zinc-500">Latest approved estimate: {money(costing.latestApprovedEstimateTotal)}</div>
            <div className="text-zinc-500">Latest draft estimate: {money(costing.latestDraftEstimateTotal)}</div>
            <div className="text-zinc-500">Draft invoice total: {money(costing.draftInvoiceTotal)}</div>
            <div className="text-zinc-500">Posted invoice total: {money(costing.postedInvoiceTotal)}</div>
          </div>
        </div>
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Estimates & Invoices</div>
        <div className="grid gap-4 lg:grid-cols-2">
          <div className="overflow-auto">
            <Table>
              <thead>
                <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                  <th className="py-2 pr-3">Estimate</th>
                  <th className="py-2 pr-3">Revision</th>
                  <th className="py-2 pr-3">Status</th>
                  <th className="py-2 pr-3">Total</th>
                </tr>
              </thead>
              <tbody>
                {costing.estimates.map((estimate) => (
                  <tr key={estimate.id} className="border-b border-zinc-100 dark:border-zinc-900">
                    <td className="py-2 pr-3">
                      <TransactionLink referenceType="SE" referenceId={estimate.id} monospace>
                        {estimate.number}
                      </TransactionLink>
                    </td>
                    <td className="py-2 pr-3">{estimate.revisionNumber}</td>
                    <td className="py-2 pr-3">{estimateStatusLabel[estimate.status] ?? estimate.status}</td>
                    <td className="py-2 pr-3">{money(estimate.total)}</td>
                  </tr>
                ))}
                {costing.estimates.length === 0 ? (
                  <tr>
                    <td className="py-6 text-sm text-zinc-500" colSpan={4}>
                      No service estimates yet.
                    </td>
                  </tr>
                ) : null}
              </tbody>
            </Table>
          </div>

          <div className="overflow-auto">
            <Table>
              <thead>
                <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                  <th className="py-2 pr-3">Invoice</th>
                  <th className="py-2 pr-3">Status</th>
                  <th className="py-2 pr-3">Total</th>
                </tr>
              </thead>
              <tbody>
                {costing.invoices.map((invoice) => (
                  <tr key={invoice.id} className="border-b border-zinc-100 dark:border-zinc-900">
                    <td className="py-2 pr-3">
                      <TransactionLink referenceType="INV" referenceId={invoice.id} monospace>
                        {invoice.number}
                      </TransactionLink>
                    </td>
                    <td className="py-2 pr-3">{invoiceStatusLabel[invoice.status] ?? invoice.status}</td>
                    <td className="py-2 pr-3">{money(invoice.total)}</td>
                  </tr>
                ))}
                {costing.invoices.length === 0 ? (
                  <tr>
                    <td className="py-6 text-sm text-zinc-500" colSpan={3}>
                      No linked sales invoices yet.
                    </td>
                  </tr>
                ) : null}
              </tbody>
            </Table>
          </div>
        </div>
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Cost Sources</div>
        <div className="space-y-4">
          <div className="overflow-auto">
            <div className="mb-2 text-sm font-medium">Material Consumption</div>
            <Table>
              <thead>
                <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                  <th className="py-2 pr-3">Source</th>
                  <th className="py-2 pr-3">Item</th>
                  <th className="py-2 pr-3">Qty</th>
                  <th className="py-2 pr-3">Unit Cost</th>
                  <th className="py-2 pr-3">Total</th>
                </tr>
              </thead>
              <tbody>
                {costing.materialLines.map((line) => (
                  <tr key={`${line.materialRequisitionId}-${line.itemId}-${line.quantity}`} className="border-b border-zinc-100 dark:border-zinc-900">
                    <td className="py-2 pr-3">
                      <TransactionLink referenceType="MR" referenceId={line.materialRequisitionId} monospace>
                        {line.materialRequisitionNumber}
                      </TransactionLink>
                    </td>
                    <td className="py-2 pr-3">
                      <ItemInlineLink itemId={line.itemId}>{line.itemSku} - {line.itemName}</ItemInlineLink>
                    </td>
                    <td className="py-2 pr-3">{line.quantity}</td>
                    <td className="py-2 pr-3">{money(line.unitCost)}</td>
                    <td className="py-2 pr-3">{money(line.lineTotal)}</td>
                  </tr>
                ))}
                {costing.materialLines.length === 0 ? (
                  <tr>
                    <td className="py-6 text-sm text-zinc-500" colSpan={5}>
                      No posted material consumption yet.
                    </td>
                  </tr>
                ) : null}
              </tbody>
            </Table>
          </div>

          <div className="overflow-auto">
            <div className="mb-2 text-sm font-medium">Direct Purchases</div>
            <Table>
              <thead>
                <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                  <th className="py-2 pr-3">Source</th>
                  <th className="py-2 pr-3">Supplier</th>
                  <th className="py-2 pr-3">Item</th>
                  <th className="py-2 pr-3">Qty</th>
                  <th className="py-2 pr-3">Line Total</th>
                </tr>
              </thead>
              <tbody>
                {costing.directPurchaseLines.map((line) => (
                  <tr key={`${line.directPurchaseId}-${line.itemId}-${line.quantity}`} className="border-b border-zinc-100 dark:border-zinc-900">
                    <td className="py-2 pr-3">
                      <TransactionLink referenceType="DPR" referenceId={line.directPurchaseId} monospace>
                        {line.directPurchaseNumber}
                      </TransactionLink>
                    </td>
                    <td className="py-2 pr-3 text-zinc-500">{line.supplierCode}</td>
                    <td className="py-2 pr-3">
                      <ItemInlineLink itemId={line.itemId}>{line.itemSku} - {line.itemName}</ItemInlineLink>
                    </td>
                    <td className="py-2 pr-3">{line.quantity}</td>
                    <td className="py-2 pr-3">{money(line.lineTotal)}</td>
                  </tr>
                ))}
                {costing.directPurchaseLines.length === 0 ? (
                  <tr>
                    <td className="py-6 text-sm text-zinc-500" colSpan={5}>
                      No posted direct purchases linked to this job.
                    </td>
                  </tr>
                ) : null}
              </tbody>
            </Table>
          </div>

          <div className="overflow-auto">
            <div className="mb-2 text-sm font-medium">Work-Order Labor Entries</div>
            <Table>
              <thead>
                <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                  <th className="py-2 pr-3">Date</th>
                  <th className="py-2 pr-3">Work Order</th>
                  <th className="py-2 pr-3">Technician</th>
                  <th className="py-2 pr-3">Work</th>
                  <th className="py-2 pr-3">Status</th>
                  <th className="py-2 pr-3">Hours</th>
                  <th className="py-2 pr-3">Labor Cost</th>
                  <th className="py-2 pr-3">Billable</th>
                  <th className="py-2 pr-3">Invoice</th>
                </tr>
              </thead>
              <tbody>
                {costing.laborLines.map((line) => (
                  <tr key={line.timeEntryId} className="border-b border-zinc-100 dark:border-zinc-900">
                    <td className="py-2 pr-3">{new Date(line.workDate).toLocaleDateString()}</td>
                    <td className="py-2 pr-3">
                      <TransactionLink referenceType="WO" referenceId={line.workOrderId} monospace>
                        {line.workOrderId.slice(0, 8)}
                      </TransactionLink>
                    </td>
                    <td className="py-2 pr-3">{line.technicianName}</td>
                    <td className="py-2 pr-3 text-zinc-500">{line.workDescription}</td>
                    <td className="py-2 pr-3">{laborStatusLabel[line.status] ?? line.status}</td>
                    <td className="py-2 pr-3">{line.hoursWorked.toFixed(2)}</td>
                    <td className="py-2 pr-3">{money(line.laborCost)}</td>
                    <td className="py-2 pr-3">
                      {line.billableToCustomer ? `${line.billableHours.toFixed(2)} hrs / ${money(line.effectiveBillableTotal)}` : "No"}
                      {line.billableToCustomer && line.effectiveBillableTotal !== line.billableTotal ? (
                        <div className="text-xs text-zinc-500">Covered from {money(line.billableTotal)}</div>
                      ) : null}
                    </td>
                    <td className="py-2 pr-3">
                      {line.salesInvoiceId ? (
                        <TransactionLink referenceType="INV" referenceId={line.salesInvoiceId} monospace>
                          {line.salesInvoiceId.slice(0, 8)}
                        </TransactionLink>
                      ) : (
                        "-"
                      )}
                    </td>
                  </tr>
                ))}
                {costing.laborLines.length === 0 ? (
                  <tr>
                    <td className="py-6 text-sm text-zinc-500" colSpan={9}>
                      No work-order labor entries linked to this job yet.
                    </td>
                  </tr>
                ) : null}
              </tbody>
            </Table>
          </div>

          <div className="overflow-auto">
            <div className="mb-2 text-sm font-medium">Expense Claims</div>
            <Table>
              <thead>
                <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                  <th className="py-2 pr-3">Claim</th>
                  <th className="py-2 pr-3">Status</th>
                  <th className="py-2 pr-3">Description</th>
                  <th className="py-2 pr-3">Billable</th>
                  <th className="py-2 pr-3">Estimate</th>
                  <th className="py-2 pr-3">Line Total</th>
                </tr>
              </thead>
              <tbody>
                {costing.expenseClaimLines.map((line, index) => (
                  <tr key={`${line.expenseClaimId}-${index}`} className="border-b border-zinc-100 dark:border-zinc-900">
                    <td className="py-2 pr-3">
                      <TransactionLink referenceType="SEC" referenceId={line.expenseClaimId} monospace>
                        {line.expenseClaimNumber}
                      </TransactionLink>
                    </td>
                    <td className="py-2 pr-3">{claimStatusLabel[line.status] ?? line.status}</td>
                    <td className="py-2 pr-3 text-zinc-500">
                      {line.itemId ? (
                        <ItemInlineLink itemId={line.itemId}>
                          {(line.itemSku ?? line.itemId)}{line.itemName ? ` - ${line.itemName}` : ""}
                        </ItemInlineLink>
                      ) : null}
                      <div>{line.description}</div>
                    </td>
                    <td className="py-2 pr-3">{line.billableToCustomer ? "Yes" : "No"}</td>
                    <td className="py-2 pr-3">
                      {line.convertedToServiceEstimateId ? (
                        <TransactionLink referenceType="SE" referenceId={line.convertedToServiceEstimateId}>
                          Linked
                        </TransactionLink>
                      ) : (
                        "-"
                      )}
                    </td>
                    <td className="py-2 pr-3">{money(line.lineTotal)}</td>
                  </tr>
                ))}
                {costing.expenseClaimLines.length === 0 ? (
                  <tr>
                    <td className="py-6 text-sm text-zinc-500" colSpan={6}>
                      No service expense claims linked to this job.
                    </td>
                  </tr>
                ) : null}
              </tbody>
            </Table>
          </div>
        </div>
      </Card>

      <Card>
        <div className="text-sm text-zinc-500">
          Track the job flow via <Link className="underline" href="/service/estimates">Estimates</Link>,{" "}
          <Link className="underline" href="/service/expense-claims">Expense Claims</Link>,{" "}
          <Link className="underline" href="/service/work-orders">Work Orders</Link>,{" "}
          <Link className="underline" href="/service/material-requisitions">Material Reqs</Link>,{" "}
          <Link className="underline" href="/procurement/direct-purchases">Direct Purchases</Link>,{" "}
          <Link className="underline" href="/service/quality-checks">Quality Checks</Link>, and{" "}
          <Link className="underline" href="/service/handovers">Handovers</Link>.
        </div>
      </Card>

      <DocumentCollaborationPanel referenceType="SJ" referenceId={id} />
    </div>
  );
}
