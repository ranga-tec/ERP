import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { ItemInlineLink } from "@/components/InlineLink";
import { Card, SecondaryLink, Table } from "@/components/ui";
import { ServiceJobActions } from "../ServiceJobActions";
import { ServiceJobEditForm } from "../ServiceJobEditForm";
import { ServiceJobAssignmentActions } from "../ServiceJobAssignmentActions";
import { ServiceJobAssignmentAddForm } from "../ServiceJobAssignmentAddForm";
import { ServiceJobDailyExpenseClaimCreateForm } from "../ServiceJobDailyExpenseClaimCreateForm";
import { ServiceJobDailyIouCreateForm } from "../ServiceJobDailyIouCreateForm";
import { ServiceJobDailyMaterialRequisitionCreateForm } from "../ServiceJobDailyMaterialRequisitionCreateForm";
import { ServiceJobDailySheetActions } from "../ServiceJobDailySheetActions";
import { ServiceJobDailySheetCreateForm } from "../ServiceJobDailySheetCreateForm";
import { ServiceJobFinalInvoiceActions } from "../ServiceJobFinalInvoiceActions";
import { ServiceJobMaterialDispositionAddForm } from "../ServiceJobMaterialDispositionAddForm";
import { ServiceJobProgressUpdateAddForm } from "../ServiceJobProgressUpdateAddForm";
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
  estimatedStartAt?: string | null;
  actualStartAt?: string | null;
  completedAt?: string | null;
  expectedCompletionAt?: string | null;
  siteLocation?: string | null;
  jobDescription?: string | null;
  customerComplaint?: string | null;
  internalRemarks?: string | null;
  responsibleOfficerName?: string | null;
  finalInvoiceNotRequired: boolean;
  finalInvoiceNotRequiredReason?: string | null;
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
type TechnicianDto = {
  id: string;
  code: string;
  name: string;
  defaultCostRate: number;
  defaultBillingRate: number;
  isActive: boolean;
};
type WarehouseDto = { id: string; code: string; name: string };
type ServiceJobDailySheetDto = {
  id: string;
  number: string;
  serviceJobId: string;
  sheetDate: string;
  preparedByName: string;
  siteLocation?: string | null;
  shiftName?: string | null;
  weatherOrSiteCondition?: string | null;
  workPlanned: string;
  workCompleted?: string | null;
  workPending?: string | null;
  problemsFound?: string | null;
  customerInstructions?: string | null;
  technicianNotes?: string | null;
  supervisorNotes?: string | null;
  status: number;
  assignmentCount: number;
  progressCount: number;
  materialRequisitionCount: number;
  materialDispositionCount: number;
  expenseClaimCount: number;
  iouCount: number;
};
type ServiceJobAssignmentDto = {
  id: string;
  serviceJobId: string;
  serviceJobDailySheetId?: string | null;
  technicianId?: string | null;
  employeeName: string;
  role: string;
  assignedTask: string;
  assignedDate: string;
  workStartAt?: string | null;
  workEndAt?: string | null;
  normalHours: number;
  overtimeHours: number;
  dailyWorkDescription?: string | null;
  approvalStatus: number;
  approvedAt?: string | null;
  rejectedAt?: string | null;
  rejectionReason?: string | null;
};
type ServiceJobProgressUpdateDto = {
  id: string;
  serviceJobId: string;
  serviceJobDailySheetId?: string | null;
  progressDate: string;
  workCompleted: string;
  workPending?: string | null;
  problemsFound?: string | null;
  additionalPartsRequired?: string | null;
  additionalLaborRequired?: string | null;
  customerInstructions?: string | null;
  siteIssues?: string | null;
  technicianNotes?: string | null;
  supervisorNotes?: string | null;
  createdAt: string;
};
type ServiceJobCloseoutCheckDto = {
  key: string;
  label: string;
  isClear: boolean;
  pendingCount: number;
  detail: string;
};
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
    materialRequisitionLineId: string;
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
type ServiceJobMaterialDispositionDto = {
  id: string;
  serviceJobDailySheetId?: string | null;
  materialRequisitionLineId: string;
  itemId: string;
  kind: number;
  quantity: number;
  unitCost: number;
  costImpact: number;
  batchNumber?: string | null;
  condition: string;
  reason: string;
  chargeTo: number;
  supplierReturnId?: string | null;
  responsiblePerson?: string | null;
  serials: string[];
  createdAt: string;
};

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
const materialDispositionLabel: Record<number, string> = {
  0: "Used",
  1: "Unused returned",
  2: "Incorrect returned",
  3: "Damaged",
  4: "Rejected / supplier return",
};
const materialChargeToLabel: Record<number, string> = {
  0: "Customer",
  1: "Company",
  2: "Supplier",
  3: "Employee",
  4: "Warranty",
};

const assignmentStatusLabel: Record<number, string> = {
  0: "Pending",
  1: "Approved",
  2: "Rejected",
};
const dailySheetStatusLabel: Record<number, string> = {
  0: "Draft",
  1: "Submitted",
  2: "Approved",
  3: "Rejected",
};

function money(value?: number | null) {
  return typeof value === "number" ? value.toFixed(2) : "-";
}

function maybeText(value?: string | null) {
  return value && value.trim().length > 0 ? value : "-";
}

export default async function ServiceJobDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [job, units, customers, costing, items, technicians, warehouses, dailySheets, assignments, progressUpdates, closeoutChecks, materialDispositions] = await Promise.all([
    backendFetchJson<ServiceJobDto>(`/service/jobs/${id}`),
    backendFetchJson<EquipmentUnitDto[]>("/service/equipment-units?take=2000"),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<ServiceJobCostingDto>(`/service/jobs/${id}/costing`),
    backendFetchJson<ItemDto[]>("/items/options"),
    backendFetchJson<TechnicianDto[]>("/service/technicians"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<ServiceJobDailySheetDto[]>(`/service/jobs/${id}/daily-sheets`),
    backendFetchJson<ServiceJobAssignmentDto[]>(`/service/jobs/${id}/assignments`),
    backendFetchJson<ServiceJobProgressUpdateDto[]>(`/service/jobs/${id}/progress-updates`),
    backendFetchJson<ServiceJobCloseoutCheckDto[]>(`/service/jobs/${id}/closeout-checks`),
    backendFetchJson<ServiceJobMaterialDispositionDto[]>(`/service/jobs/${id}/material-dispositions`),
  ]);

  const selectedUnit =
    units.some((unit) => unit.id === job.equipmentUnitId)
      ? null
      : await backendFetchJson<EquipmentUnitDto>(`/service/equipment-units/${job.equipmentUnitId}`).catch(() => null);
  const availableUnits = selectedUnit ? [selectedUnit, ...units] : units;

  const unitById = new Map(availableUnits.map((u) => [u.id, u]));
  const customerById = new Map(customers.map((c) => [c.id, c]));
  const itemById = new Map(items.map((item) => [item.id, item]));
  const equipmentUnitOptions = availableUnits.map((unit) => {
    const item = itemById.get(unit.itemId);
    const customer = customerById.get(unit.customerId);
    return {
      ...unit,
      itemSku: item?.sku,
      itemName: item?.name,
      customerCode: customer?.code,
    };
  });

  const canStart = job.status === 0 || job.status === 1 || job.status === 2 || job.status === 13;
  const canComplete = job.status === 1 || job.status === 2 || job.status === 3 || job.status === 13;
  const canClose = job.status === 7 || job.status === 10 || job.status === 11;
  const canReopen = job.status === 12;
  const canEditHeader = job.status === 0 || job.status === 1 || job.status === 13;
  const canAddJobActivity = job.status !== 12 && job.status !== 14;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/service/jobs" className="hover:underline">
            Job Orders
          </Link>{" "}
          / <span className="font-mono text-xs">{job.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Job Order {job.number}</h1>
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
          <div>Est. start: {job.estimatedStartAt ? new Date(job.estimatedStartAt).toLocaleDateString() : "-"}</div>
          <div>Actual start: {job.actualStartAt ? new Date(job.actualStartAt).toLocaleString() : "-"}</div>
          <div>Expected: {job.expectedCompletionAt ? new Date(job.expectedCompletionAt).toLocaleDateString() : "-"}</div>
          <div>Completed: {job.completedAt ? new Date(job.completedAt).toLocaleString() : "-"}</div>
          <div>Site: {job.siteLocation ?? "-"}</div>
          <div>Responsible: {job.responsibleOfficerName ?? "-"}</div>
          <div>Invoice required: {job.finalInvoiceNotRequired ? "No" : "Yes"}</div>
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
        <ServiceJobActions jobId={job.id} canStart={canStart} canComplete={canComplete} canClose={canClose} canReopen={canReopen} />
      </Card>

      {canEditHeader ? (
        <Card>
          <div className="mb-3 text-sm font-semibold">Edit Job</div>
          <ServiceJobEditForm job={job} equipmentUnits={equipmentUnitOptions} customers={customers} />
        </Card>
      ) : (
        <Card>
          <div className="text-sm text-zinc-500">
            Job header fields are editable while the job is draft, open, or reopened. After work starts, use the job status flow and linked service documents instead of changing the intake header.
          </div>
        </Card>
      )}

      <Card>
        <div className="mb-2 text-sm font-semibold">Job Intake</div>
        <div className="grid gap-4 md:grid-cols-2">
          <div>
            <div className="text-xs uppercase tracking-wide text-zinc-500">Job Description</div>
            <div className="mt-1 whitespace-pre-wrap text-sm text-zinc-700 dark:text-zinc-200">{job.jobDescription ?? "-"}</div>
          </div>
          <div>
            <div className="text-xs uppercase tracking-wide text-zinc-500">Customer Complaint</div>
            <div className="mt-1 whitespace-pre-wrap text-sm text-zinc-700 dark:text-zinc-200">{job.customerComplaint ?? "-"}</div>
          </div>
          <div>
            <div className="text-xs uppercase tracking-wide text-zinc-500">Problem / Intake Note</div>
            <div className="mt-1 whitespace-pre-wrap text-sm text-zinc-700 dark:text-zinc-200">{job.problemDescription}</div>
          </div>
          <div>
            <div className="text-xs uppercase tracking-wide text-zinc-500">Internal Remarks</div>
            <div className="mt-1 whitespace-pre-wrap text-sm text-zinc-700 dark:text-zinc-200">{job.internalRemarks ?? "-"}</div>
          </div>
        </div>
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

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div>
            <div className="text-sm font-semibold">Closeout Readiness</div>
            <div className="mt-1 text-xs text-zinc-500">Clear these operational and financial items before closing the job.</div>
          </div>
          <div className="text-sm font-medium">
            {closeoutChecks.every((check) => check.isClear) ? "Ready to close" : `${closeoutChecks.filter((check) => !check.isClear).length} pending`}
          </div>
        </div>
        <div className="grid gap-2 md:grid-cols-2">
          {closeoutChecks.map((check) => (
            <div key={check.key} className="rounded-lg border border-[var(--card-border)] p-3">
              <div className="flex items-center justify-between gap-3">
                <div className="text-sm font-medium">{check.label}</div>
                <div className={check.isClear ? "text-xs font-semibold text-emerald-700 dark:text-emerald-300" : "text-xs font-semibold text-amber-700 dark:text-amber-300"}>
                  {check.isClear ? "Clear" : `${check.pendingCount} pending`}
                </div>
              </div>
              <div className="mt-1 text-xs text-zinc-500">{check.detail}</div>
            </div>
          ))}
        </div>
        {job.finalInvoiceNotRequiredReason ? (
          <div className="mt-3 text-xs text-zinc-500">Not billable reason: {job.finalInvoiceNotRequiredReason}</div>
        ) : null}
        <div className="mt-3">
          <ServiceJobFinalInvoiceActions jobId={job.id} disabled={job.status === 12 || job.finalInvoiceNotRequired} />
        </div>
      </Card>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div>
            <div className="text-sm font-semibold">Daily Field Sheets</div>
            <div className="mt-1 text-xs text-zinc-500">Capture each working day before replacing paper job sheets, cash notes, and material return notes.</div>
          </div>
        </div>
        <ServiceJobDailySheetCreateForm serviceJobId={job.id} disabled={!canAddJobActivity} />
        <div className="mt-4 overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Sheet</th>
                <th className="py-2 pr-3">Date</th>
                <th className="py-2 pr-3">Prepared By</th>
                <th className="py-2 pr-3">Work</th>
                <th className="py-2 pr-3">Entries</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {dailySheets.map((sheet) => (
                <tr key={sheet.id} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">{sheet.number}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(sheet.sheetDate).toLocaleString()}</td>
                  <td className="py-2 pr-3">{sheet.preparedByName}</td>
                  <td className="py-2 pr-3">
                    <div>{sheet.workPlanned}</div>
                    {sheet.workCompleted ? <div className="mt-1 text-xs text-zinc-500">Done: {sheet.workCompleted}</div> : null}
                    {sheet.workPending ? <div className="mt-1 text-xs text-zinc-500">Pending: {sheet.workPending}</div> : null}
                  </td>
                  <td className="py-2 pr-3 text-xs text-zinc-500">
                    <div>Staff {sheet.assignmentCount} / Progress {sheet.progressCount}</div>
                    <div>MRN {sheet.materialRequisitionCount} / Returns {sheet.materialDispositionCount}</div>
                    <div>Expenses {sheet.expenseClaimCount} / IOU {sheet.iouCount}</div>
                  </td>
                  <td className="py-2 pr-3">{dailySheetStatusLabel[sheet.status] ?? sheet.status}</td>
                  <td className="py-2 pr-3">
                    <ServiceJobDailySheetActions serviceJobId={job.id} dailySheetId={sheet.id} status={sheet.status} />
                  </td>
                </tr>
              ))}
              {dailySheets.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={7}>No daily field sheets recorded yet.</td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Daily Cash, Expense, And Material Actions</div>
        <div className="space-y-5">
          <div>
            <div className="mb-2 text-sm font-medium">Issue IOU / petty cash advance</div>
            <ServiceJobDailyIouCreateForm serviceJobId={job.id} dailySheets={dailySheets} disabled={!canAddJobActivity} />
          </div>
          <div>
            <div className="mb-2 text-sm font-medium">Record out-of-pocket or petty-cash expense voucher</div>
            <ServiceJobDailyExpenseClaimCreateForm serviceJobId={job.id} dailySheets={dailySheets} disabled={!canAddJobActivity} />
          </div>
          <div>
            <div className="mb-2 text-sm font-medium">Request materials, spare parts, lubricants, or consumables</div>
            <ServiceJobDailyMaterialRequisitionCreateForm serviceJobId={job.id} dailySheets={dailySheets} warehouses={warehouses} disabled={!canAddJobActivity} />
          </div>
        </div>
      </Card>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div>
            <div className="text-sm font-semibold">Technician / Worker Assignments</div>
            <div className="mt-1 text-xs text-zinc-500">Assign service staff to this job and approve daily assignment records before closeout.</div>
          </div>
          <Link className="text-sm font-semibold text-[var(--link)] underline underline-offset-2" href="/service/technicians">
            Technician Master
          </Link>
        </div>
        <div className="mb-4">
          <ServiceJobAssignmentAddForm serviceJobId={job.id} technicians={technicians} dailySheets={dailySheets} disabled={!canAddJobActivity} />
        </div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Assigned</th>
                <th className="py-2 pr-3">Employee</th>
                <th className="py-2 pr-3">Role</th>
                <th className="py-2 pr-3">Task</th>
                <th className="py-2 pr-3">Hours</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {assignments.map((assignment) => (
                <tr key={assignment.id} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                  <td className="py-2 pr-3 text-zinc-500">
                    <div>{new Date(assignment.assignedDate).toLocaleString()}</div>
                    <div className="text-xs">
                      {assignment.workStartAt ? new Date(assignment.workStartAt).toLocaleTimeString() : "-"} - {assignment.workEndAt ? new Date(assignment.workEndAt).toLocaleTimeString() : "-"}
                    </div>
                  </td>
                  <td className="py-2 pr-3">{assignment.employeeName}</td>
                  <td className="py-2 pr-3">{assignment.role}</td>
                  <td className="py-2 pr-3">
                    <div>{assignment.assignedTask}</div>
                    {assignment.dailyWorkDescription ? <div className="mt-1 text-xs text-zinc-500">{assignment.dailyWorkDescription}</div> : null}
                    {assignment.rejectionReason ? <div className="mt-1 text-xs text-red-600 dark:text-red-300">{assignment.rejectionReason}</div> : null}
                  </td>
                  <td className="py-2 pr-3">
                    <div>Normal {assignment.normalHours.toFixed(2)}</div>
                    <div className="text-xs text-zinc-500">OT {assignment.overtimeHours.toFixed(2)}</div>
                  </td>
                  <td className="py-2 pr-3">{assignmentStatusLabel[assignment.approvalStatus] ?? assignment.approvalStatus}</td>
                  <td className="py-2 pr-3">
                    <ServiceJobAssignmentActions serviceJobId={job.id} assignmentId={assignment.id} status={assignment.approvalStatus} />
                  </td>
                </tr>
              ))}
              {assignments.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={7}>
                    No technicians or workers assigned yet.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Daily Job Progress</div>
        <div className="mb-4">
          <ServiceJobProgressUpdateAddForm serviceJobId={job.id} dailySheets={dailySheets} disabled={!canAddJobActivity} />
        </div>
        <div className="space-y-3">
          {progressUpdates.map((update) => (
            <div key={update.id} className="rounded-lg border border-[var(--card-border)] p-3">
              <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
                <div className="text-sm font-medium">{new Date(update.progressDate).toLocaleString()}</div>
                <div className="text-xs text-zinc-500">Created {new Date(update.createdAt).toLocaleString()}</div>
              </div>
              <div className="grid gap-3 md:grid-cols-2 text-sm">
                <div><span className="font-medium">Completed:</span> {maybeText(update.workCompleted)}</div>
                <div><span className="font-medium">Pending:</span> {maybeText(update.workPending)}</div>
                <div><span className="font-medium">Problems:</span> {maybeText(update.problemsFound)}</div>
                <div><span className="font-medium">Parts required:</span> {maybeText(update.additionalPartsRequired)}</div>
                <div><span className="font-medium">Labor required:</span> {maybeText(update.additionalLaborRequired)}</div>
                <div><span className="font-medium">Customer instructions:</span> {maybeText(update.customerInstructions)}</div>
                <div><span className="font-medium">Site issues:</span> {maybeText(update.siteIssues)}</div>
                <div><span className="font-medium">Technician notes:</span> {maybeText(update.technicianNotes)}</div>
                <div><span className="font-medium">Supervisor notes:</span> {maybeText(update.supervisorNotes)}</div>
              </div>
            </div>
          ))}
          {progressUpdates.length === 0 ? (
            <div className="text-sm text-zinc-500">No daily progress updates recorded yet.</div>
          ) : null}
        </div>
      </Card>

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
        <div className="mb-3 text-sm font-semibold">Profitability Report</div>
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
        <div className="mb-3 text-sm font-semibold">Quotations & Final Invoices</div>
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
            <div className="mb-3">
              <ServiceJobMaterialDispositionAddForm serviceJobId={job.id} materialLines={costing.materialLines} dailySheets={dailySheets} disabled={!canAddJobActivity} />
            </div>
            <Table>
              <thead>
                <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                  <th className="py-2 pr-3">Source</th>
                  <th className="py-2 pr-3">Item</th>
                  <th className="py-2 pr-3">Qty</th>
                  <th className="py-2 pr-3">Unit Cost</th>
                  <th className="py-2 pr-3">Total</th>
                  <th className="py-2 pr-3">Disposition</th>
                </tr>
              </thead>
              <tbody>
                {costing.materialLines.map((line) => {
                  const dispositionsForLine = materialDispositions.filter((disposition) => disposition.materialRequisitionLineId === line.materialRequisitionLineId);
                  return (
                  <tr key={`${line.materialRequisitionId}-${line.materialRequisitionLineId}-${line.itemId}-${line.quantity}`} className="border-b border-zinc-100 align-top dark:border-zinc-900">
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
                    <td className="py-2 pr-3 text-xs text-zinc-500">
                      {dispositionsForLine.length > 0 ? dispositionsForLine.map((disposition) => (
                        <div key={disposition.id} className="mb-1">
                          {materialDispositionLabel[disposition.kind] ?? disposition.kind}: {disposition.quantity} ({materialChargeToLabel[disposition.chargeTo] ?? disposition.chargeTo})
                          <div>{disposition.reason}</div>
                        </div>
                      )) : "Pending"}
                    </td>
                  </tr>
                  );
                })}
                {costing.materialLines.length === 0 ? (
                  <tr>
                    <td className="py-6 text-sm text-zinc-500" colSpan={6}>
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
                  <th className="py-2 pr-3">Job Detail / Job Sheet</th>
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
            <div className="mb-2 text-sm font-medium">Petty Cash</div>
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
          Track the job flow via <Link className="underline" href="/service/estimates">Quotations</Link>,{" "}
          <Link className="underline" href="/service/expense-claims">Petty Cash</Link>,{" "}
          <Link className="underline" href="/service/work-orders">Job Detail / Job Sheet</Link>,{" "}
          <Link className="underline" href="/service/material-requisitions">MRN</Link>,{" "}
          <Link className="underline" href="/procurement/direct-purchases">Direct Purchases</Link>,{" "}
          <Link className="underline" href="/service/quality-checks">Inspection / QC</Link>, and{" "}
          <Link className="underline" href="/service/handovers">Service Taken / Delivery Confirmation</Link>.
        </div>
      </Card>

      <DocumentCollaborationPanel referenceType="SJ" referenceId={id} />
    </div>
  );
}
