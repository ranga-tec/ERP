import type { ReactNode } from "react";
import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { ItemInlineLink } from "@/components/InlineLink";
import { Card, SecondaryLink, Table } from "@/components/ui";
import { JobFormModal } from "../JobFormModal";
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
import { ServiceJobMaterialDispositionActions } from "../ServiceJobMaterialDispositionActions";
import { ServiceJobOperationActions } from "../ServiceJobOperationActions";
import { ServiceJobOperationAddForm } from "../ServiceJobOperationAddForm";
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
type ServiceJobOperationDto = {
  id: string;
  serviceJobId: string;
  sequence: number;
  name: string;
  description?: string | null;
  plannedItemId?: string | null;
  plannedItemSku?: string | null;
  plannedItemName?: string | null;
  plannedQuantity: number;
  estimatedLaborHours: number;
  requiredAt?: string | null;
  notes?: string | null;
  status: number;
  startedAt?: string | null;
  completedAt?: string | null;
  actualMaterialQuantity: number;
  actualMaterialCost: number;
  approvedLaborHours: number;
  approvedLaborCost: number;
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
    occurredAt: string;
    materialRequisitionId: string;
    materialRequisitionLineId: string;
    materialRequisitionNumber: string;
    serviceJobDailySheetId?: string | null;
    serviceJobDailySheetNumber?: string | null;
    warehouseId: string;
    warehouseCode: string;
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
  status: number;
  postedAt?: string | null;
  isVoided: boolean;
  voidedAt?: string | null;
  voidReason?: string | null;
  createdAt: string;
};
type PettyCashIouDto = {
  id: string;
  number: string;
  serviceJobId: string;
  serviceJobNumber?: string | null;
  serviceJobDailySheetId?: string | null;
  requestedByName: string;
  amount: number;
  purpose: string;
  requestedAt: string;
  expectedSettlementAt?: string | null;
  status: number;
  submittedAt?: string | null;
  approvedAt?: string | null;
  pettyCashFundId?: string | null;
  releasedAt?: string | null;
  releaseReference?: string | null;
  settledAt?: string | null;
  settledAmount?: number | null;
  settlementReference?: string | null;
  rejectionReason?: string | null;
};
type ServiceExpenseClaimSummaryDto = {
  id: string;
  number: string;
  serviceJobId: string;
  serviceJobDailySheetId?: string | null;
  claimedByUserId?: string | null;
  claimedByName: string;
  fundingSource: number;
  expenseDate: string;
  merchantName?: string | null;
  status: number;
  total: number;
  lineCount: number;
  billableUnconvertedLineCount: number;
  settledAt?: string | null;
};
type ServiceHandoverDto = {
  id: string;
  number: string;
  serviceJobId: string;
  handoverDate: string;
  status: number;
  salesInvoiceId?: string | null;
  salesInvoiceNumber?: string | null;
  convertedToInvoiceAt?: string | null;
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
const pettyCashIouStatusLabel: Record<number, string> = {
  0: "Draft",
  1: "Submitted",
  2: "Approved",
  3: "Cash Released",
  4: "Settled / Accounted",
  5: "Rejected",
  6: "Cancelled",
};
const fundingSourceLabel: Record<number, string> = {
  1: "Out of Pocket",
  2: "Petty Cash",
};
const handoverStatusLabel: Record<number, string> = {
  0: "Draft",
  1: "Completed",
  2: "Cancelled",
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
  1: "Not needed returned",
  2: "Wrongly issued returned",
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
const materialStockEffectLabel: Record<number, string> = {
  0: "Consumed / not returned",
  1: "Returned to stock",
  2: "Returned to stock",
  3: "Damaged / not available",
  4: "Returned to stock for supplier return",
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
const operationStatusLabel: Record<number, string> = {
  0: "Planned",
  1: "In Progress",
  2: "Completed",
  3: "Skipped",
};

function money(value?: number | null) {
  return typeof value === "number" ? value.toFixed(2) : "-";
}

function maybeText(value?: string | null) {
  return value && value.trim().length > 0 ? value : "-";
}

function sameLocalDate(left?: string | null, right = new Date()) {
  if (!left) return false;
  return new Date(left).toLocaleDateString() === right.toLocaleDateString();
}

function ProcessStatusBadge({ status }: { status: "complete" | "current" | "blocked" | "pending" }) {
  const label = {
    complete: "Done",
    current: "Active",
    blocked: "Blocked",
    pending: "Pending",
  }[status];
  const className = {
    complete: "border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-900/40 dark:bg-emerald-950/30 dark:text-emerald-200",
    current: "border-blue-200 bg-blue-50 text-blue-800 dark:border-blue-900/40 dark:bg-blue-950/30 dark:text-blue-200",
    blocked: "border-amber-200 bg-amber-50 text-amber-800 dark:border-amber-900/40 dark:bg-amber-950/30 dark:text-amber-200",
    pending: "border-zinc-200 bg-zinc-50 text-zinc-600 dark:border-zinc-800 dark:bg-zinc-900/40 dark:text-zinc-300",
  }[status];

  return <span className={`rounded-full border px-2 py-0.5 text-[11px] font-semibold ${className}`}>{label}</span>;
}

type WorkflowTone = "neutral" | "good" | "warn" | "bad";
type WorkflowMeta = { label: string; value: string | number; tone?: WorkflowTone };
type WorkflowBar = { key: string; title: string; tone: WorkflowTone };

function workflowToneClass(tone: WorkflowTone) {
  return {
    neutral: "border-zinc-200 bg-zinc-50 text-zinc-600 dark:border-zinc-800 dark:bg-zinc-900/40 dark:text-zinc-300",
    good: "border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-900/40 dark:bg-emerald-950/30 dark:text-emerald-200",
    warn: "border-amber-200 bg-amber-50 text-amber-800 dark:border-amber-900/40 dark:bg-amber-950/30 dark:text-amber-200",
    bad: "border-red-200 bg-red-50 text-red-800 dark:border-red-900/40 dark:bg-red-950/30 dark:text-red-200",
  }[tone];
}

function workflowBarClass(tone: WorkflowTone) {
  return {
    neutral: "bg-zinc-300 dark:bg-zinc-700",
    good: "bg-emerald-500",
    warn: "bg-amber-500",
    bad: "bg-red-500",
  }[tone];
}

function WorkflowMetaPill({ meta }: { meta: WorkflowMeta }) {
  return (
    <span className={`rounded-full border px-2 py-0.5 text-[11px] font-semibold ${workflowToneClass(meta.tone ?? "neutral")}`}>
      {meta.value} {meta.label}
    </span>
  );
}

function CockpitMetric({
  label,
  value,
  detail,
  href,
  tone = "neutral",
}: {
  label: string;
  value: string;
  detail: string;
  href?: string;
  tone?: "neutral" | "good" | "warn";
}) {
  const toneClass =
    tone === "good"
      ? "border-emerald-200 bg-emerald-50/70 dark:border-emerald-900/40 dark:bg-emerald-950/20"
      : tone === "warn"
        ? "border-amber-200 bg-amber-50/70 dark:border-amber-900/40 dark:bg-amber-950/20"
        : "border-[var(--card-border)] bg-[var(--card-bg)]";
  const content = (
    <>
      <div className="text-[11px] uppercase tracking-wide text-zinc-500">{label}</div>
      <div className="mt-0.5 text-base font-semibold">{value}</div>
      <div className="mt-1 text-xs text-zinc-500">{detail}</div>
    </>
  );

  if (href) {
    return (
      <Link href={href} className={`rounded-md border p-2 transition hover:border-[var(--link)] hover:bg-[var(--surface-soft)] ${toneClass}`}>
        {content}
      </Link>
    );
  }

  return <div className={`rounded-md border p-2 ${toneClass}`}>{content}</div>;
}

function CollapsibleCard({
  title,
  summary,
  meta,
  defaultOpen = false,
  children,
}: {
  title: string;
  summary?: string;
  meta?: string;
  defaultOpen?: boolean;
  children: ReactNode;
}) {
  return (
    <details
      open={defaultOpen}
      className="rounded-lg border border-[var(--card-border)] bg-[var(--card-bg)] p-3 shadow-[var(--shadow-card)] transition-colors duration-150"
    >
      <summary className="cursor-pointer list-none">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <div className="text-sm font-semibold">{title}</div>
            {summary ? <div className="mt-1 text-xs text-zinc-500">{summary}</div> : null}
          </div>
          {meta ? <div className="text-xs font-medium text-zinc-500">{meta}</div> : null}
        </div>
      </summary>
      <div className="mt-4">{children}</div>
    </details>
  );
}

function ServiceJobExpenseClaimRegister({
  claims,
  dailySheetNumberById,
  emptyMessage,
}: {
  claims: ServiceExpenseClaimSummaryDto[];
  dailySheetNumberById: Map<string, string>;
  emptyMessage: string;
}) {
  return (
    <div className="mt-5 border-t border-[var(--card-border)] pt-4">
      <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
        <div>
          <div className="text-sm font-semibold">Job Expense Register</div>
          <div className="mt-1 text-xs text-zinc-500">Expense vouchers created from this job stay visible here with their approval and settlement status.</div>
        </div>
        <Link className="text-xs font-semibold text-[var(--link)] underline underline-offset-2" href="/service/expense-claims">
          Open all claims
        </Link>
      </div>
      <div className="overflow-auto">
        <Table>
          <thead>
            <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
              <th className="py-2 pr-3">Claim</th>
              <th className="py-2 pr-3">Daily Sheet</th>
              <th className="py-2 pr-3">Claimed By</th>
              <th className="py-2 pr-3">Funding</th>
              <th className="py-2 pr-3">Date</th>
              <th className="py-2 pr-3">Status</th>
              <th className="py-2 pr-3 text-right">Total</th>
              <th className="py-2 pr-3">Lines</th>
            </tr>
          </thead>
          <tbody>
            {claims.map((claim) => (
              <tr key={claim.id} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                <td className="py-2 pr-3 font-mono text-xs">
                  <Link className="hover:underline" href={`/service/expense-claims/${claim.id}`}>
                    {claim.number}
                  </Link>
                </td>
                <td className="py-2 pr-3 font-mono text-xs">
                  {claim.serviceJobDailySheetId ? dailySheetNumberById.get(claim.serviceJobDailySheetId) ?? "Linked" : "Unlinked"}
                </td>
                <td className="py-2 pr-3">{claim.claimedByName}</td>
                <td className="py-2 pr-3">{fundingSourceLabel[claim.fundingSource] ?? claim.fundingSource}</td>
                <td className="py-2 pr-3 text-zinc-500">{new Date(claim.expenseDate).toLocaleString()}</td>
                <td className="py-2 pr-3">
                  {claimStatusLabel[claim.status] ?? claim.status}
                  {claim.settledAt ? <div className="mt-1 text-xs text-zinc-500">Settled {new Date(claim.settledAt).toLocaleDateString()}</div> : null}
                </td>
                <td className="py-2 pr-3 text-right">{money(claim.total)}</td>
                <td className="py-2 pr-3 text-xs text-zinc-500">
                  <div>{claim.lineCount} line{claim.lineCount === 1 ? "" : "s"}</div>
                  {claim.billableUnconvertedLineCount > 0 ? (
                    <div className="text-amber-700 dark:text-amber-300">{claim.billableUnconvertedLineCount} billable not converted</div>
                  ) : null}
                </td>
              </tr>
            ))}
            {claims.length === 0 ? (
              <tr>
                <td className="py-6 text-sm text-zinc-500" colSpan={8}>
                  {emptyMessage}
                </td>
              </tr>
            ) : null}
          </tbody>
        </Table>
      </div>
    </div>
  );
}

type JobTabKey = "overview" | "plan" | "daily-work" | "materials" | "expenses" | "billing" | "costs" | "files";
type DailyWorkViewKey = "sheets" | "labor" | "progress";
type MaterialViewKey = "issues" | "returns" | "damage";
type ExpenseViewKey = "ious" | "petty-cash" | "reimbursements";

const jobTabs: { key: JobTabKey; label: string }[] = [
  { key: "overview", label: "Overview" },
  { key: "plan", label: "Plan" },
  { key: "daily-work", label: "Daily Work" },
  { key: "materials", label: "Materials" },
  { key: "expenses", label: "Expenses" },
  { key: "billing", label: "Billing" },
  { key: "costs", label: "Costs" },
  { key: "files", label: "Files & Notes" },
];

const dailyWorkViews: { key: DailyWorkViewKey; label: string }[] = [
  { key: "sheets", label: "Daily Sheets" },
  { key: "labor", label: "Staff / Labor" },
  { key: "progress", label: "Progress" },
];
const materialViews: { key: MaterialViewKey; label: string }[] = [
  { key: "issues", label: "Issued MRNs" },
  { key: "returns", label: "Return Materials" },
  { key: "damage", label: "Damage Material" },
];
const expenseViews: { key: ExpenseViewKey; label: string }[] = [
  { key: "ious", label: "IOU Advances" },
  { key: "petty-cash", label: "Petty Cash Expenses" },
  { key: "reimbursements", label: "Out-of-Pocket Claims" },
];

function resolveJobTab(value?: string): JobTabKey {
  return jobTabs.some((tab) => tab.key === value) ? (value as JobTabKey) : "overview";
}

function resolveDailyWorkView(value?: string): DailyWorkViewKey {
  return dailyWorkViews.some((view) => view.key === value) ? (value as DailyWorkViewKey) : "sheets";
}

function resolveMaterialView(value?: string): MaterialViewKey {
  return materialViews.some((view) => view.key === value) ? (value as MaterialViewKey) : "issues";
}

function resolveExpenseView(value?: string): ExpenseViewKey {
  return expenseViews.some((view) => view.key === value) ? (value as ExpenseViewKey) : "ious";
}

function tabHref(jobId: string, tab: JobTabKey) {
  return tab === "overview" ? `/service/jobs/${jobId}#tab-content` : `/service/jobs/${jobId}?tab=${tab}#tab-content`;
}

function dailyWorkHref(jobId: string, view: DailyWorkViewKey = "sheets", dailySheetId?: string) {
  return `/service/jobs/${jobId}?tab=daily-work&dailyView=${view}${dailySheetId ? `&dailySheetId=${dailySheetId}` : ""}#tab-content`;
}

function materialHref(jobId: string, view: MaterialViewKey = "issues") {
  return `/service/jobs/${jobId}?tab=materials&materialView=${view}#tab-content`;
}

function expenseHref(jobId: string, view: ExpenseViewKey = "ious") {
  return `/service/jobs/${jobId}?tab=expenses&expenseView=${view}#tab-content`;
}

function closeoutCheckHref(jobId: string, key: string) {
  const targets: Record<string, string> = {
    "daily-field-sheets": dailyWorkHref(jobId, "sheets"),
    "expense-claims": expenseHref(jobId, "petty-cash"),
    "petty-cash-ious": expenseHref(jobId, "ious"),
    "direct-purchase-bills": "/procurement/direct-purchases",
    "material-requisitions": tabHref(jobId, "materials"),
    "job-assignments": dailyWorkHref(jobId, "labor"),
    "labor-entries": "/service/work-orders",
    "work-orders": "/service/work-orders",
    "billable-labor": tabHref(jobId, "billing"),
    "material-disposition": tabHref(jobId, "materials"),
    "final-invoice": tabHref(jobId, "billing"),
  };

  return targets[key] ?? tabHref(jobId, "overview");
}

export default async function ServiceJobDetailPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams?: Promise<{ tab?: string; dailyView?: string; dailySheetId?: string; materialView?: string; expenseView?: string }>;
}) {
  const { id } = await params;
  const resolvedSearchParams = await searchParams;
  const activeTab = resolveJobTab(resolvedSearchParams?.tab);
  const activeDailyWorkView = resolveDailyWorkView(resolvedSearchParams?.dailyView);
  const activeMaterialView = resolveMaterialView(resolvedSearchParams?.materialView);
  const activeExpenseView = resolveExpenseView(resolvedSearchParams?.expenseView);

  const [
    job,
    units,
    customers,
    costing,
    items,
    technicians,
    warehouses,
    dailySheets,
    assignments,
    progressUpdates,
    closeoutChecks,
    materialDispositions,
    operations,
    pettyCashIous,
    expenseClaims,
    handovers,
  ] = await Promise.all([
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
    backendFetchJson<ServiceJobOperationDto[]>(`/service/jobs/${id}/operations`),
    backendFetchJson<PettyCashIouDto[]>(`/finance/petty-cash-ious?serviceJobId=${id}&take=100`),
    backendFetchJson<ServiceExpenseClaimSummaryDto[]>(`/service/expense-claims?serviceJobId=${id}&take=100`),
    backendFetchJson<ServiceHandoverDto[]>("/service/handovers?take=500"),
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
  const nextOperationSequence = operations.length > 0 ? Math.max(...operations.map((operation) => operation.sequence)) + 10 : 10;
  const selectedDailySheet = dailySheets.find((sheet) => sheet.id === resolvedSearchParams?.dailySheetId) ?? dailySheets[0] ?? null;
  const selectedDailySheets = selectedDailySheet ? [selectedDailySheet] : [];
  const selectedAssignments = selectedDailySheet
    ? assignments.filter((assignment) => assignment.serviceJobDailySheetId === selectedDailySheet.id)
    : [];
  const selectedProgressUpdates = selectedDailySheet
    ? progressUpdates.filter((update) => update.serviceJobDailySheetId === selectedDailySheet.id)
    : [];
  const selectedDailySheetLocked = selectedDailySheet?.status === 2;
  const dailySheetCreateDisabledReason = !canAddJobActivity
    ? "Daily sheets cannot be added after the job is closed or cancelled. Reopen the job before recording another day."
    : undefined;
  const selectedDailySheetLockReason = selectedDailySheetLocked
    ? `${selectedDailySheet.number} is approved. Create or select another daily sheet before recording new labor or progress.`
    : undefined;
  const dailySheetNumberById = new Map(dailySheets.map((sheet) => [sheet.id, sheet.number]));
  const materialLineById = new Map(costing.materialLines.map((line) => [line.materialRequisitionLineId, line]));
  const issuedMaterialGroups = [...costing.materialLines.reduce((groups, line) => {
    const existing = groups.get(line.materialRequisitionId);
    if (existing) {
      existing.lines.push(line);
      existing.quantity += line.quantity;
      existing.total += line.lineTotal;
      return groups;
    }

    groups.set(line.materialRequisitionId, {
      id: line.materialRequisitionId,
      number: line.materialRequisitionNumber,
      occurredAt: line.occurredAt,
      dailySheetId: line.serviceJobDailySheetId,
      dailySheetNumber: line.serviceJobDailySheetNumber,
      warehouseCode: line.warehouseCode,
      quantity: line.quantity,
      total: line.lineTotal,
      lines: [line],
    });
    return groups;
  }, new Map<string, {
    id: string;
    number: string;
    occurredAt: string;
    dailySheetId?: string | null;
    dailySheetNumber?: string | null;
    warehouseCode: string;
    quantity: number;
    total: number;
    lines: ServiceJobCostingDto["materialLines"];
  }>()).values()];
  const returnMaterialDispositions = materialDispositions.filter((disposition) => disposition.kind === 1 || disposition.kind === 2 || disposition.kind === 4);
  const damageMaterialDispositions = materialDispositions.filter((disposition) => disposition.kind === 3);
  const jobHandovers = handovers.filter((handover) => handover.serviceJobId === job.id);
  const latestHandover = jobHandovers[0] ?? null;
  const latestProgress = [...progressUpdates].sort((a, b) => new Date(b.progressDate).getTime() - new Date(a.progressDate).getTime())[0] ?? null;
  const todayAssignments = assignments.filter((assignment) => sameLocalDate(assignment.assignedDate));
  const pendingIous = pettyCashIous.filter((iou) => ![4, 5, 6].includes(iou.status));
  const pendingClaims = expenseClaims.filter((claim) => ![3, 4].includes(claim.status));
  const pendingCloseoutCount = closeoutChecks.reduce((total, check) => total + (check.isClear ? 0 : check.pendingCount), 0);
  const pendingMaterialDisposition = closeoutChecks.find((check) => check.key === "material-disposition")?.pendingCount ?? 0;
  const hasApprovedEstimate = costing.estimates.some((estimate) => estimate.status === 1);
  const hasDraftEstimate = costing.estimates.some((estimate) => estimate.status === 0);
  const hasPostedInvoice = costing.invoices.some((invoice) => invoice.status === 1 || invoice.status === 2);
  const hasDraftInvoice = costing.invoices.some((invoice) => invoice.status === 0);
  const completedOperations = operations.filter((operation) => operation.status === 2).length;
  const activeOperations = operations.filter((operation) => operation.status === 1).length;
  const approvedDailySheets = dailySheets.filter((sheet) => sheet.status === 2).length;
  const pendingDailySheets = dailySheets.filter((sheet) => sheet.status !== 2).length;
  const approvedAssignments = assignments.filter((assignment) => assignment.approvalStatus === 1).length;
  const pendingAssignments = assignments.filter((assignment) => assignment.approvalStatus === 0).length;
  const latestProgressDate = latestProgress ? new Date(latestProgress.progressDate).toLocaleDateString() : "-";
  const approvedEstimates = costing.estimates.filter((estimate) => estimate.status === 1).length;
  const postedInvoices = costing.invoices.filter((invoice) => invoice.status === 1 || invoice.status === 2).length;
  const dailySheetBars: WorkflowBar[] = dailySheets.slice(0, 12).map((sheet) => ({
    key: sheet.id,
    title: `${sheet.number} - ${dailySheetStatusLabel[sheet.status] ?? sheet.status}`,
    tone: sheet.status === 2 ? "good" : sheet.status === 1 ? "warn" : sheet.status === 3 ? "bad" : "neutral",
  }));
  const processSteps: {
    label: string;
    detail: string;
    href: string;
    status: "complete" | "current" | "blocked" | "pending";
    meta: WorkflowMeta[];
    bars?: WorkflowBar[];
  }[] = [
    {
      label: "Intake",
      detail: statusLabel[job.status] ?? String(job.status),
      href: tabHref(job.id, "overview"),
      status: job.openedAt ? "complete" : "current",
      meta: [
        { label: "status", value: statusLabel[job.status] ?? job.status, tone: job.status === 12 ? "good" : job.status === 14 ? "bad" : "neutral" },
        { label: "opened", value: new Date(job.openedAt).toLocaleDateString() },
      ],
    },
    {
      label: "Plan",
      detail: operations.length === 0 ? "No operations planned" : `${completedOperations}/${operations.length} completed`,
      href: tabHref(job.id, "plan"),
      status: operations.length === 0 ? "pending" : operations.every((operation) => operation.status === 2 || operation.status === 3) ? "complete" : "current",
      meta: [
        { label: "ops", value: operations.length },
        { label: "active", value: activeOperations, tone: activeOperations > 0 ? "warn" : "neutral" },
        { label: "done", value: completedOperations, tone: completedOperations > 0 ? "good" : "neutral" },
      ],
    },
    {
      label: "Daily Sheets",
      detail: dailySheets.length === 0 ? "No daily sheets" : `${approvedDailySheets}/${dailySheets.length} approved`,
      href: dailyWorkHref(job.id, "sheets"),
      status: dailySheets.length === 0 ? "pending" : dailySheets.every((sheet) => sheet.status === 2) ? "complete" : "current",
      meta: [
        { label: "sheets", value: dailySheets.length },
        { label: "approved", value: approvedDailySheets, tone: approvedDailySheets > 0 ? "good" : "neutral" },
        { label: "open", value: pendingDailySheets, tone: pendingDailySheets > 0 ? "warn" : "neutral" },
      ],
      bars: dailySheetBars,
    },
    {
      label: "Labour",
      detail: assignments.length === 0 ? "No labour entries" : `${approvedAssignments}/${assignments.length} approved`,
      href: dailyWorkHref(job.id, "labor", selectedDailySheet?.id),
      status: assignments.length === 0 ? "pending" : pendingAssignments > 0 ? "blocked" : "complete",
      meta: [
        { label: "entries", value: assignments.length },
        { label: "approved", value: approvedAssignments, tone: approvedAssignments > 0 ? "good" : "neutral" },
        { label: "pending", value: pendingAssignments, tone: pendingAssignments > 0 ? "warn" : "neutral" },
      ],
    },
    {
      label: "Progress",
      detail: progressUpdates.length === 0 ? "No progress updates" : `Latest ${latestProgressDate}`,
      href: dailyWorkHref(job.id, "progress", selectedDailySheet?.id),
      status: progressUpdates.length === 0 ? "pending" : canAddJobActivity ? "current" : "complete",
      meta: [
        { label: "updates", value: progressUpdates.length, tone: progressUpdates.length > 0 ? "good" : "neutral" },
        { label: "latest", value: latestProgressDate },
      ],
    },
    {
      label: "Materials",
      detail: pendingMaterialDisposition > 0 ? `${pendingMaterialDisposition} pending disposition` : `${costing.materialLines.length} issued lines`,
      href: materialHref(job.id, pendingMaterialDisposition > 0 ? "returns" : "issues"),
      status: pendingMaterialDisposition > 0 ? "blocked" : costing.materialLines.length > 0 ? "complete" : "pending",
      meta: [
        { label: "issued", value: costing.materialLines.length },
        { label: "pending", value: pendingMaterialDisposition, tone: pendingMaterialDisposition > 0 ? "warn" : "neutral" },
      ],
    },
    {
      label: "Expenses",
      detail: pendingIous.length + pendingClaims.length > 0 ? `${pendingIous.length + pendingClaims.length} pending finance items` : `${pettyCashIous.length + expenseClaims.length} tracked`,
      href: expenseHref(job.id, pendingIous.length > 0 ? "ious" : "petty-cash"),
      status: pendingIous.length + pendingClaims.length > 0 ? "blocked" : pettyCashIous.length + expenseClaims.length > 0 ? "complete" : "pending",
      meta: [
        { label: "IOUs", value: pettyCashIous.length },
        { label: "claims", value: expenseClaims.length },
        { label: "pending", value: pendingIous.length + pendingClaims.length, tone: pendingIous.length + pendingClaims.length > 0 ? "warn" : "neutral" },
      ],
    },
    {
      label: "Quote",
      detail: hasApprovedEstimate ? "Approved quotation exists" : hasDraftEstimate ? "Draft quotation exists" : "No quotation",
      href: tabHref(job.id, "billing"),
      status: hasApprovedEstimate ? "complete" : hasDraftEstimate ? "current" : "pending",
      meta: [
        { label: "quotes", value: costing.estimates.length },
        { label: "approved", value: approvedEstimates, tone: approvedEstimates > 0 ? "good" : "neutral" },
      ],
    },
    {
      label: "Service Taken",
      detail: latestHandover ? `${latestHandover.number} / ${handoverStatusLabel[latestHandover.status] ?? latestHandover.status}` : "Not created",
      href: "/service/handovers",
      status: latestHandover?.status === 1 ? "complete" : latestHandover ? "current" : "pending",
      meta: [
        { label: "records", value: jobHandovers.length },
        { label: "latest", value: latestHandover ? (handoverStatusLabel[latestHandover.status] ?? latestHandover.status) : "-", tone: latestHandover?.status === 1 ? "good" : latestHandover ? "warn" : "neutral" },
      ],
    },
    {
      label: "Invoice",
      detail: hasPostedInvoice ? "Posted invoice exists" : hasDraftInvoice ? "Draft invoice exists" : job.finalInvoiceNotRequired ? "Not billable" : "Not invoiced",
      href: tabHref(job.id, "billing"),
      status: hasPostedInvoice || job.finalInvoiceNotRequired ? "complete" : hasDraftInvoice ? "current" : "pending",
      meta: [
        { label: "invoices", value: costing.invoices.length },
        { label: "posted", value: postedInvoices, tone: postedInvoices > 0 || job.finalInvoiceNotRequired ? "good" : "neutral" },
        { label: "draft", value: costing.invoices.length - postedInvoices, tone: hasDraftInvoice ? "warn" : "neutral" },
      ],
    },
    {
      label: "Close",
      detail: pendingCloseoutCount === 0 ? "Ready when status allows" : `${pendingCloseoutCount} blocker${pendingCloseoutCount === 1 ? "" : "s"}`,
      href: tabHref(job.id, "overview"),
      status: job.status === 12 ? "complete" : pendingCloseoutCount > 0 ? "blocked" : "current",
      meta: [
        { label: "blockers", value: pendingCloseoutCount, tone: pendingCloseoutCount > 0 ? "warn" : "good" },
      ],
    },
  ];
  const actualCostBreakdown = [
    {
      label: "Materials",
      amount: costing.materialConsumedCost,
      detail: `${costing.materialLines.length} posted MRN line${costing.materialLines.length === 1 ? "" : "s"}`,
      href: "#job-cost-materials",
    },
    {
      label: "Direct purchases",
      amount: costing.directPurchaseCost,
      detail: `${costing.directPurchaseLines.length} posted direct purchase line${costing.directPurchaseLines.length === 1 ? "" : "s"}`,
      href: "#job-cost-direct-purchases",
    },
    {
      label: "Approved labor",
      amount: costing.approvedLaborCost,
      detail: `${costing.laborLines.filter((line) => line.status === 2 || line.status === 4).length} approved/invoiced work-order labor entr${costing.laborLines.filter((line) => line.status === 2 || line.status === 4).length === 1 ? "y" : "ies"}`,
      href: "#job-cost-labor",
    },
    {
      label: "Approved claims",
      amount: costing.approvedExpenseClaimCost,
      detail: `${costing.expenseClaimLines.filter((line) => line.status === 2 || line.status === 3).length} approved/settled petty cash line${costing.expenseClaimLines.filter((line) => line.status === 2 || line.status === 3).length === 1 ? "" : "s"}`,
      href: "#job-cost-petty-cash",
    },
  ];
  const dailySheetSummaries = dailySheets.map((sheet) => {
    const sheetAssignments = assignments.filter((assignment) => assignment.serviceJobDailySheetId === sheet.id);
    const sheetProgress = progressUpdates.filter((update) => update.serviceJobDailySheetId === sheet.id);
    const latestSheetProgress = sheetProgress.reduce<ServiceJobProgressUpdateDto | null>((latest, update) => {
      if (!latest) return update;
      return new Date(update.progressDate).getTime() > new Date(latest.progressDate).getTime() ? update : latest;
    }, null);
    const materialLineCount = costing.materialLines.filter((line) => line.serviceJobDailySheetId === sheet.id).length;
    const materialDispositionCount = materialDispositions.filter((disposition) => disposition.serviceJobDailySheetId === sheet.id).length;
    const iouCount = pettyCashIous.filter((iou) => iou.serviceJobDailySheetId === sheet.id).length;
    const claimCount = expenseClaims.filter((claim) => claim.serviceJobDailySheetId === sheet.id).length;
    const staffPreview = sheetAssignments.slice(0, 3).map((assignment) => assignment.employeeName).join(", ");

    return {
      sheet,
      assignmentCount: sheetAssignments.length,
      progressCount: sheetProgress.length,
      materialLineCount,
      materialDispositionCount,
      iouCount,
      claimCount,
      staffPreview: staffPreview || "No staff recorded",
      latestProgressDate: latestSheetProgress ? new Date(latestSheetProgress.progressDate).toLocaleString() : null,
      latestProgressDetail: latestSheetProgress ? maybeText(latestSheetProgress.workCompleted) : "No progress update yet",
    };
  });

  return (
    <div className="space-y-4">
      <div>
        <div className="text-xs text-zinc-500">
          <Link href="/service/jobs" className="hover:underline">Job Orders</Link>{" "}
          / <span className="font-mono">{job.number}</span>
        </div>
        <div className="mt-1 flex flex-wrap items-start justify-between gap-3">
          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-2">
              <h1 className="text-xl font-semibold">Job Order {job.number}</h1>
              <span className={[
                "rounded-full border px-2 py-0.5 text-xs font-semibold",
                job.status === 12 ? "border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-900/40 dark:bg-emerald-950/30 dark:text-emerald-200"
                : job.status === 14 ? "border-red-200 bg-red-50 text-red-800 dark:border-red-900/40 dark:bg-red-950/30 dark:text-red-200"
                : job.status === 3 || job.status === 2 || job.status === 1 ? "border-blue-200 bg-blue-50 text-blue-800 dark:border-blue-900/40 dark:bg-blue-950/30 dark:text-blue-200"
                : "border-zinc-200 bg-zinc-50 text-zinc-600 dark:border-zinc-800 dark:bg-zinc-900/40 dark:text-zinc-300",
              ].join(" ")}>
                {statusLabel[job.status] ?? job.status}
              </span>
              <span className="rounded border border-[var(--card-border)] px-1.5 py-0.5 text-xs text-zinc-500">{kindLabel[job.kind] ?? job.kind}</span>
            </div>
            <div className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-0.5 text-xs text-zinc-500">
              <span>Equipment: <TransactionLink referenceType="EUNIT" referenceId={job.equipmentUnitId} monospace>{unitById.get(job.equipmentUnitId)?.serialNumber ?? job.equipmentUnitId}</TransactionLink></span>
              <span>Customer: {customerById.get(job.customerId)?.code ?? job.customerId}</span>
              {job.siteLocation ? <span>Site: {job.siteLocation}</span> : null}
              {job.responsibleOfficerName ? <span>Responsible: {job.responsibleOfficerName}</span> : null}
            </div>
            <details>
              <summary className="mt-0.5 cursor-pointer list-none text-xs text-[var(--link)] hover:underline">Show dates &amp; details ▾</summary>
              <div className="mt-1.5 flex flex-wrap gap-x-3 gap-y-0.5 text-xs text-zinc-500">
                <span>Opened: {new Date(job.openedAt).toLocaleString()}</span>
                <span>Est. start: {job.estimatedStartAt ? new Date(job.estimatedStartAt).toLocaleDateString() : "-"}</span>
                <span>Actual start: {job.actualStartAt ? new Date(job.actualStartAt).toLocaleString() : "-"}</span>
                <span>Expected: {job.expectedCompletionAt ? new Date(job.expectedCompletionAt).toLocaleDateString() : "-"}</span>
                <span>Completed: {job.completedAt ? new Date(job.completedAt).toLocaleString() : "-"}</span>
                <span>Invoice required: {job.finalInvoiceNotRequired ? "No" : "Yes"}</span>
              </div>
            </details>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <SecondaryLink
              className="min-h-7 px-2 py-1 text-xs"
              href={`/api/backend/service/jobs/${job.id}/pdf`}
              target="_blank"
              rel="noopener noreferrer"
            >
              PDF
            </SecondaryLink>
            <ServiceJobActions jobId={job.id} canStart={canStart} canComplete={canComplete} canClose={canClose} canReopen={canReopen} compact />
          </div>
        </div>
      </div>

      <nav id="tab-content" className="overflow-x-auto border-b border-[var(--card-border)]">
        <div className="flex min-w-max gap-1">
          {jobTabs.map((tab) => {
            const active = tab.key === activeTab;
            return (
              <Link
                key={tab.key}
                href={tabHref(job.id, tab.key)}
                className={[
                  "border-b-2 px-3 py-2 text-sm font-medium transition-colors",
                  active
                    ? "border-[var(--link)] text-[var(--link)]"
                    : "border-transparent text-zinc-500 hover:border-zinc-300 hover:text-[var(--foreground)]",
                ].join(" ")}
              >
                {tab.label}
              </Link>
            );
          })}
        </div>
      </nav>

      {activeTab === "overview" ? (
        <>
      <Card className="p-2">
        <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
          <div>
            <div className="text-sm font-semibold">Job Cockpit</div>
          </div>
          <div className={pendingCloseoutCount > 0 ? "text-xs font-semibold text-amber-700 dark:text-amber-300" : "text-xs font-semibold text-emerald-700 dark:text-emerald-300"}>
            {pendingCloseoutCount > 0 ? `${pendingCloseoutCount} pending closeout blocker${pendingCloseoutCount === 1 ? "" : "s"}` : "No closeout blockers"}
          </div>
        </div>

        <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-4 xl:grid-cols-8">
          <CockpitMetric
            label="Last Progress"
            value={latestProgress ? new Date(latestProgress.progressDate).toLocaleDateString() : "None"}
            detail={latestProgress ? maybeText(latestProgress.workCompleted) : "No progress update recorded yet"}
            href={dailyWorkHref(job.id, "progress", selectedDailySheet?.id)}
            tone={latestProgress ? "good" : "warn"}
          />
          <CockpitMetric
            label="Staff Today"
            value={String(todayAssignments.length)}
            detail={todayAssignments.length > 0 ? todayAssignments.map((assignment) => assignment.employeeName).slice(0, 2).join(", ") : "No staff assigned today"}
            href={dailyWorkHref(job.id, "labor", selectedDailySheet?.id)}
            tone={todayAssignments.length > 0 ? "good" : "neutral"}
          />
          <CockpitMetric
            label="Cash / Expenses"
            value={String(pendingIous.length + pendingClaims.length)}
            detail={pendingIous.length + pendingClaims.length > 0 ? "Pending approval, release, settlement, or rejection" : "No pending finance items"}
            href={expenseHref(job.id, pendingIous.length > 0 ? "ious" : "petty-cash")}
            tone={pendingIous.length + pendingClaims.length > 0 ? "warn" : "good"}
          />
          <CockpitMetric
            label="Uninvoiced Labour"
            value={money(costing.uninvoicedBillableLaborRevenue)}
            detail="Approved billable labour not yet in a final invoice"
            href={tabHref(job.id, "billing")}
            tone={costing.uninvoicedBillableLaborRevenue > 0 ? "warn" : "good"}
          />
          <CockpitMetric
            label="Material Disposition"
            value={String(pendingMaterialDisposition)}
            detail={pendingMaterialDisposition > 0 ? "Issued materials still need final disposition" : "No pending material disposition"}
            href={materialHref(job.id, "returns")}
            tone={pendingMaterialDisposition > 0 ? "warn" : "good"}
          />
          <CockpitMetric
            label="Service Taken"
            value={latestHandover ? latestHandover.number : "Not created"}
            detail={latestHandover ? handoverStatusLabel[latestHandover.status] ?? String(latestHandover.status) : "Create when repair/service is handed over"}
            href={latestHandover ? `/service/handovers/${latestHandover.id}` : "/service/handovers"}
            tone={latestHandover?.status === 1 ? "good" : "neutral"}
          />
          <CockpitMetric
            label="Invoice"
            value={hasPostedInvoice ? "Posted" : hasDraftInvoice ? "Draft" : job.finalInvoiceNotRequired ? "Not billable" : "Pending"}
            detail={costing.invoices[0]?.number ?? "No linked invoice yet"}
            href={tabHref(job.id, "billing")}
            tone={hasPostedInvoice || job.finalInvoiceNotRequired ? "good" : hasDraftInvoice ? "neutral" : "warn"}
          />
          <CockpitMetric
            label="Job Cost"
            value={money(costing.totalActualCost)}
            detail={`Posted revenue ${money(costing.postedInvoiceTotal)}`}
            href={tabHref(job.id, "costs")}
          />
        </div>
      </Card>

      <Card>
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <div>
            <div className="text-sm font-semibold">Process Timeline</div>
            <div className="mt-1 text-xs text-zinc-500">Click a stage to work in that focused area without searching through the full page.</div>
          </div>
          <div className="text-xs font-medium text-zinc-500">{pendingCloseoutCount > 0 ? `${pendingCloseoutCount} closeout blocker${pendingCloseoutCount === 1 ? "" : "s"}` : "Closeout clear"}</div>
        </div>
        <div className="grid gap-2 md:grid-cols-2 xl:grid-cols-5">
          {processSteps.map((step) => (
            <Link key={step.label} href={step.href} className="flex min-h-[108px] flex-col rounded-lg border border-[var(--card-border)] p-2.5 transition hover:border-[var(--link)] hover:bg-[var(--surface-soft)]">
              <div className="flex items-center justify-between gap-2">
                <div className="text-sm font-medium">{step.label}</div>
                <ProcessStatusBadge status={step.status} />
              </div>
              <div className="mt-1 text-xs text-zinc-500">{step.detail}</div>
              <div className="mt-2 flex flex-wrap gap-1.5">
                {step.meta.map((meta) => (
                  <WorkflowMetaPill key={`${step.label}-${meta.label}`} meta={meta} />
                ))}
              </div>
              {step.bars && step.bars.length > 0 ? (
                <div className="mt-auto pt-3">
                  <div className="flex h-2 overflow-hidden rounded-full bg-zinc-100 dark:bg-zinc-900">
                    {step.bars.map((bar) => (
                      <span key={bar.key} title={bar.title} className={`min-w-2 flex-1 ${workflowBarClass(bar.tone)}`} />
                    ))}
                  </div>
                  {dailySheets.length > step.bars.length ? (
                    <div className="mt-1 text-[11px] text-zinc-500">Showing {step.bars.length} of {dailySheets.length}</div>
                  ) : null}
                </div>
              ) : null}
            </Link>
          ))}
        </div>
      </Card>

      {canEditHeader ? (
        <CollapsibleCard title="Edit Job" summary="Change intake header fields while the job is still editable.">
          <ServiceJobEditForm job={job} equipmentUnits={equipmentUnitOptions} customers={customers} />
        </CollapsibleCard>
      ) : (
        <CollapsibleCard title="Edit Job" summary="Header fields are locked after work starts.">
          <div className="text-sm text-zinc-500">
            Job header fields are editable while the job is draft, open, or reopened. After work starts, use the job status flow and linked service documents instead of changing the intake header.
          </div>
        </CollapsibleCard>
      )}

      <CollapsibleCard title="Job Intake" summary="Original customer requirement, problem note, scope, and internal remarks.">
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
      </CollapsibleCard>

        </>
      ) : null}

      {activeTab === "plan" ? (
      <Card>
        <div className="mb-3 flex flex-wrap items-start justify-between gap-2">
          <div>
            <div className="text-sm font-semibold">Job Operations / Sub-Parts Plan</div>
            <div className="mt-1 text-xs text-zinc-500">Plan complex repair stages, expected sub-parts, labor, and due dates before issuing actual MRNs or recording labor.</div>
          </div>
          <JobFormModal title="Add Operation / Sub-Part" description="Plan a repair step, expected part, labor estimate, and due date." buttonLabel="+ Add Operation" variant="secondary" disabled={!canAddJobActivity}>
            <ServiceJobOperationAddForm serviceJobId={job.id} items={items} nextSequence={nextOperationSequence} disabled={!canAddJobActivity} />
          </JobFormModal>
        </div>
        <div className="mt-4 overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Step No.</th>
                <th className="py-2 pr-3">Work Step / Subassembly</th>
                <th className="py-2 pr-3">Planned Part</th>
                <th className="py-2 pr-3">Plan</th>
                <th className="py-2 pr-3">Actual</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {operations.map((operation) => (
                <tr key={operation.id} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                  <td className="py-2 pr-3 text-xs font-medium">{operation.sequence}</td>
                  <td className="py-2 pr-3">
                    <div className="text-sm font-medium">{operation.name}</div>
                    {operation.description ? <div className="mt-1 text-xs text-zinc-500">{operation.description}</div> : null}
                    {operation.notes ? <div className="mt-1 text-xs text-zinc-500">Notes: {operation.notes}</div> : null}
                    {operation.requiredAt ? <div className="mt-1 text-xs text-zinc-500">Required: {new Date(operation.requiredAt).toLocaleDateString()}</div> : null}
                  </td>
                  <td className="py-2 pr-3 text-sm">
                    {operation.plannedItemId ? (
                      <ItemInlineLink itemId={operation.plannedItemId}>
                        {operation.plannedItemSku ?? operation.plannedItemId}
                        {operation.plannedItemName ? ` - ${operation.plannedItemName}` : ""}
                      </ItemInlineLink>
                    ) : (
                      "-"
                    )}
                  </td>
                  <td className="py-2 pr-3 text-xs text-zinc-500">
                    <div>Qty {operation.plannedQuantity}</div>
                    <div>Labor {operation.estimatedLaborHours.toFixed(2)} hrs</div>
                  </td>
                  <td className="py-2 pr-3 text-xs text-zinc-500">
                    <div>Material {operation.actualMaterialQuantity} / {money(operation.actualMaterialCost)}</div>
                    <div>Labor {operation.approvedLaborHours.toFixed(2)} hrs / {money(operation.approvedLaborCost)}</div>
                  </td>
                  <td className="py-2 pr-3 text-sm">
                    <div>{operationStatusLabel[operation.status] ?? operation.status}</div>
                    {operation.startedAt ? <div className="mt-1 text-xs text-zinc-500">Started {new Date(operation.startedAt).toLocaleString()}</div> : null}
                    {operation.completedAt ? <div className="mt-1 text-xs text-zinc-500">Done {new Date(operation.completedAt).toLocaleString()}</div> : null}
                  </td>
                  <td className="py-2 pr-3">
                    <ServiceJobOperationActions serviceJobId={job.id} operationId={operation.id} status={operation.status} disabled={!canAddJobActivity} />
                  </td>
                </tr>
              ))}
              {operations.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={7}>No planned job operations or sub-parts yet.</td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>
      ) : null}

      {activeTab === "billing" ? (
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
            <Link key={check.key} href={closeoutCheckHref(job.id, check.key)} className="rounded-lg border border-[var(--card-border)] p-3 transition hover:border-[var(--link)] hover:bg-[var(--surface-soft)]">
              <div className="flex items-center justify-between gap-3">
                <div className="text-sm font-medium">{check.label}</div>
                <div className={check.isClear ? "text-xs font-semibold text-emerald-700 dark:text-emerald-300" : "text-xs font-semibold text-amber-700 dark:text-amber-300"}>
                  {check.isClear ? "Clear" : `${check.pendingCount} pending`}
                </div>
              </div>
              <div className="mt-1 text-xs text-zinc-500">{check.detail}</div>
              <div className="mt-2 text-xs font-semibold text-[var(--link)]">{check.isClear ? "Review" : "Open pending records"}</div>
            </Link>
          ))}
        </div>
        {job.finalInvoiceNotRequiredReason ? (
          <div className="mt-3 text-xs text-zinc-500">Not billable reason: {job.finalInvoiceNotRequiredReason}</div>
        ) : null}
        <div className="mt-3">
          <ServiceJobFinalInvoiceActions jobId={job.id} disabled={job.status === 12 || job.finalInvoiceNotRequired} />
        </div>
      </Card>
      ) : null}

      {activeTab === "daily-work" ? (
        <>
      <nav className="overflow-x-auto border-b border-[var(--card-border)]">
        <div className="flex min-w-max gap-1">
          {dailyWorkViews.map((view) => {
            const active = view.key === activeDailyWorkView;
            return (
              <Link
                key={view.key}
                href={dailyWorkHref(job.id, view.key, selectedDailySheet?.id)}
                className={[
                  "border-b-2 px-3 py-2 text-sm font-medium transition-colors",
                  active
                    ? "border-[var(--link)] text-[var(--link)]"
                    : "border-transparent text-zinc-500 hover:border-zinc-300 hover:text-[var(--foreground)]",
                ].join(" ")}
              >
                {view.label}
              </Link>
            );
          })}
        </div>
      </nav>

      {activeDailyWorkView === "sheets" ? (
      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div>
            <div className="text-sm font-semibold">Daily Field Sheets</div>
            <div className="mt-1 text-xs text-zinc-500">Capture each working day — replaces paper job sheets, cash notes, and material return notes.</div>
          </div>
          {dailySheets.length > 0 ? (
            <JobFormModal title="Add Daily Field Sheet" description="Create another daily work record for this job." buttonLabel="+ Add Another Day" variant="secondary" disabled={!canAddJobActivity}>
              <ServiceJobDailySheetCreateForm serviceJobId={job.id} disabled={!canAddJobActivity} disabledReason={dailySheetCreateDisabledReason} />
            </JobFormModal>
          ) : null}
        </div>
        {dailySheets.length === 0 ? (
          <div className="rounded-lg border-2 border-dashed border-[var(--card-border)] p-6 text-center">
            <div className="mb-3 text-sm text-zinc-500">No daily field sheets recorded yet. Start by creating your first one.</div>
            <div className="flex justify-center">
              <JobFormModal title="Create First Daily Sheet" description="Create the first daily work record before adding labour, progress, materials, IOUs, or expenses." buttonLabel="+ Create First Daily Sheet" disabled={!canAddJobActivity}>
                <ServiceJobDailySheetCreateForm serviceJobId={job.id} disabled={!canAddJobActivity} disabledReason={dailySheetCreateDisabledReason} />
              </JobFormModal>
            </div>
          </div>
        ) : null}
        <div className="mt-4 space-y-3">
          {dailySheetSummaries.map((summary) => {
            const { sheet } = summary;
            return (
              <div key={sheet.id} className="rounded-lg border border-[var(--card-border)] bg-[var(--card-bg)] p-4 shadow-[var(--shadow-soft)]">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="font-mono text-sm font-semibold">{sheet.number}</span>
                      <span className="rounded-full border border-[var(--card-border)] px-2 py-0.5 text-[11px] font-medium text-zinc-500">
                        {dailySheetStatusLabel[sheet.status] ?? sheet.status}
                      </span>
                    </div>
                    <div className="mt-1 text-xs text-zinc-500">
                      {new Date(sheet.sheetDate).toLocaleString()} / Prepared by {sheet.preparedByName}
                    </div>
                  </div>
                  <div className="flex flex-wrap gap-2 text-[11px] text-zinc-500">
                    <span className="rounded-full border border-[var(--card-border)] px-2 py-0.5">Staff {summary.assignmentCount}</span>
                    <span className="rounded-full border border-[var(--card-border)] px-2 py-0.5">Progress {summary.progressCount}</span>
                    <span className="rounded-full border border-[var(--card-border)] px-2 py-0.5">MRN {summary.materialLineCount}</span>
                    <span className="rounded-full border border-[var(--card-border)] px-2 py-0.5">Returns {summary.materialDispositionCount}</span>
                    <span className="rounded-full border border-[var(--card-border)] px-2 py-0.5">Expenses {summary.claimCount}</span>
                    <span className="rounded-full border border-[var(--card-border)] px-2 py-0.5">IOU {summary.iouCount}</span>
                  </div>
                </div>

                <div className="mt-4 grid gap-3 lg:grid-cols-3">
                  <div className="rounded-md border border-[var(--card-border)] p-3">
                    <div className="text-xs font-semibold uppercase tracking-wide text-zinc-500">Planned</div>
                    <div className="mt-2 line-clamp-4 whitespace-pre-wrap text-sm">{maybeText(sheet.workPlanned)}</div>
                  </div>
                  <div className="rounded-md border border-[var(--card-border)] p-3">
                    <div className="text-xs font-semibold uppercase tracking-wide text-zinc-500">Done</div>
                    <div className="mt-2 line-clamp-4 whitespace-pre-wrap text-sm">{maybeText(sheet.workCompleted)}</div>
                  </div>
                  <div className="rounded-md border border-[var(--card-border)] p-3">
                    <div className="text-xs font-semibold uppercase tracking-wide text-zinc-500">Pending / Issues</div>
                    <div className="mt-2 line-clamp-3 whitespace-pre-wrap text-sm">{maybeText(sheet.workPending)}</div>
                    {sheet.problemsFound ? <div className="mt-2 line-clamp-2 whitespace-pre-wrap text-xs text-amber-700 dark:text-amber-300">{sheet.problemsFound}</div> : null}
                  </div>
                </div>

                <div className="mt-4 grid gap-3 md:grid-cols-2">
                  <div className="rounded-md bg-[var(--surface-soft)] p-3 text-sm">
                    <div className="text-xs font-semibold uppercase tracking-wide text-zinc-500">Latest Activity</div>
                    <div className="mt-2 font-medium">{summary.latestProgressDate ?? "No progress date"}</div>
                    <div className="mt-1 line-clamp-2 text-xs text-zinc-500">{summary.latestProgressDetail}</div>
                  </div>
                  <div className="rounded-md bg-[var(--surface-soft)] p-3 text-sm">
                    <div className="text-xs font-semibold uppercase tracking-wide text-zinc-500">Staff Preview</div>
                    <div className="mt-2 line-clamp-2">{summary.staffPreview}</div>
                    <div className="mt-1 text-xs text-zinc-500">
                      Materials {summary.materialLineCount + summary.materialDispositionCount} / Cash {summary.iouCount + summary.claimCount}
                    </div>
                  </div>
                </div>

                <div className="mt-4 flex flex-wrap items-center gap-3 text-xs">
                  <Link className="font-semibold text-[var(--link)] underline underline-offset-2" href={dailyWorkHref(job.id, "labor", sheet.id)}>
                    Staff / labour
                  </Link>
                  <Link className="font-semibold text-[var(--link)] underline underline-offset-2" href={dailyWorkHref(job.id, "progress", sheet.id)}>
                    Progress
                  </Link>
                  <Link className="font-semibold text-[var(--link)] underline underline-offset-2" href={materialHref(job.id, "issues")}>
                    Materials
                  </Link>
                  <Link className="font-semibold text-[var(--link)] underline underline-offset-2" href={expenseHref(job.id, "ious")}>
                    Request IOU
                  </Link>
                  <Link className="font-semibold text-[var(--link)] underline underline-offset-2" href={expenseHref(job.id, "reimbursements")}>
                    Add expense
                  </Link>
                  <ServiceJobDailySheetActions serviceJobId={job.id} dailySheetId={sheet.id} status={sheet.status} />
                </div>
              </div>
            );
          })}
        </div>
      </Card>
      ) : null}

      {activeDailyWorkView !== "sheets" ? (
        selectedDailySheet ? (
          <Card>
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <div className="text-sm font-semibold">Selected Daily Sheet</div>
                <div className="mt-1 text-xs text-zinc-500">
                  {selectedDailySheet.number} / {new Date(selectedDailySheet.sheetDate).toLocaleString()} / {dailySheetStatusLabel[selectedDailySheet.status] ?? selectedDailySheet.status}
                </div>
                {selectedDailySheetLockReason ? (
                  <div className="mt-2 text-xs text-amber-700 dark:text-amber-300">{selectedDailySheetLockReason}</div>
                ) : null}
              </div>
              <div className="flex flex-wrap gap-2 text-xs text-zinc-500">
                <span>Staff {selectedAssignments.length}</span>
                <span>Progress {selectedProgressUpdates.length}</span>
                <span>MRN {selectedDailySheet.materialRequisitionCount}</span>
                <span>Returns {selectedDailySheet.materialDispositionCount}</span>
                <span>Expenses {selectedDailySheet.expenseClaimCount}</span>
                <span>IOU {selectedDailySheet.iouCount}</span>
              </div>
            </div>
            <div className="mt-4 border-t border-[var(--card-border)] pt-3">
              <div className="mb-2 text-xs font-semibold uppercase tracking-wide text-zinc-500">Select Daily Sheet</div>
              <div className="flex flex-wrap gap-2">
                {dailySheets.map((sheet) => {
                  const active = sheet.id === selectedDailySheet.id;
                  return (
                    <Link
                      key={sheet.id}
                      href={dailyWorkHref(job.id, activeDailyWorkView, sheet.id)}
                      className={[
                        "rounded-md border px-3 py-2 text-left text-xs transition-colors",
                        active
                          ? "border-[var(--link)] bg-blue-50 text-[var(--link)] dark:bg-blue-950/30"
                          : "border-[var(--card-border)] text-zinc-600 hover:border-zinc-400 dark:text-zinc-300",
                      ].join(" ")}
                    >
                      <span className="block font-mono font-semibold">{sheet.number}</span>
                      <span className="mt-1 block text-zinc-500">
                        {new Date(sheet.sheetDate).toLocaleDateString()} / {dailySheetStatusLabel[sheet.status] ?? sheet.status}
                      </span>
                    </Link>
                  );
                })}
              </div>
            </div>
          </Card>
        ) : (
          <Card>
            <div className="flex flex-col items-start gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <div className="text-sm font-semibold text-zinc-700 dark:text-zinc-200">No daily sheet selected</div>
                <div className="mt-1 text-xs text-zinc-500">
                  {activeDailyWorkView === "labor" ? "Staff and labor entries are recorded against a daily field sheet." : "Progress updates are recorded against a daily field sheet."}
                  {" "}Start by creating your first daily sheet for this job.
                </div>
              </div>
              <Link
                href={dailyWorkHref(job.id, "sheets")}
                className="inline-flex items-center rounded-md border border-[var(--link)] bg-[var(--link)] px-3 py-1.5 text-sm font-medium text-white transition hover:opacity-90"
              >
                Go to Daily Sheets
              </Link>
            </div>
          </Card>
        )
      ) : null}

      {activeDailyWorkView === "labor" && selectedDailySheet ? (
      <CollapsibleCard
        title="Daily Staff / Labor"
        summary={`Assign staff and labor for ${selectedDailySheet.number}.`}
        meta={`${selectedAssignments.length} assigned`}
        defaultOpen
      >
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <Link className="text-sm font-semibold text-[var(--link)] underline underline-offset-2" href="/service/technicians">
            Technician Master
          </Link>
          <JobFormModal title="Add Staff / Labor" description={`Assign staff and labor for ${selectedDailySheet.number}.`} buttonLabel="+ Add Staff / Labor" variant="secondary" disabled={!canAddJobActivity || !selectedDailySheet || selectedDailySheetLocked}>
            {selectedDailySheetLockReason ? (
              <div className="mb-3 rounded-md border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900 dark:border-amber-900/40 dark:bg-amber-950/30 dark:text-amber-100">
                {selectedDailySheetLockReason}{" "}
                <Link className="font-semibold underline underline-offset-2" href={dailyWorkHref(job.id, "sheets")}>
                  Open daily sheets
                </Link>
              </div>
            ) : null}
            <ServiceJobAssignmentAddForm
              serviceJobId={job.id}
              technicians={technicians}
              dailySheets={selectedDailySheets}
              defaultDailySheetId={selectedDailySheet?.id ?? ""}
              requireDailySheet
              disabled={!canAddJobActivity || !selectedDailySheet || selectedDailySheetLocked}
            />
          </JobFormModal>
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
              {selectedAssignments.map((assignment) => (
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
              {selectedAssignments.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={7}>
                    No technicians or workers assigned yet.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </CollapsibleCard>
      ) : null}

      {activeDailyWorkView === "progress" && selectedDailySheet ? (
      <CollapsibleCard
        title="Daily Progress"
        summary={`Record completed work, pending work, and issues for ${selectedDailySheet.number}.`}
        meta={`${selectedProgressUpdates.length} updates`}
        defaultOpen
      >
        <div className="space-y-3">
          {selectedProgressUpdates.map((update) => (
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
          {selectedProgressUpdates.length === 0 ? (
            <div className="text-sm text-zinc-500">No progress updates recorded for the selected daily sheet yet.</div>
          ) : null}
        </div>
        <div className="mt-4 flex justify-end">
          <JobFormModal title="Add Progress Update" description={`Record completed work, pending work, and issues for ${selectedDailySheet.number}.`} buttonLabel="+ Add Progress" variant="secondary" disabled={!canAddJobActivity || !selectedDailySheet || selectedDailySheetLocked}>
            {selectedDailySheetLockReason ? (
              <div className="mb-3 rounded-md border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900 dark:border-amber-900/40 dark:bg-amber-950/30 dark:text-amber-100">
                {selectedDailySheetLockReason}{" "}
                <Link className="font-semibold underline underline-offset-2" href={dailyWorkHref(job.id, "sheets")}>
                  Open daily sheets
                </Link>
              </div>
            ) : null}
            <ServiceJobProgressUpdateAddForm
              serviceJobId={job.id}
              dailySheets={selectedDailySheets}
              defaultDailySheetId={selectedDailySheet?.id ?? ""}
              requireDailySheet
              disabled={!canAddJobActivity || !selectedDailySheet || selectedDailySheetLocked}
            />
          </JobFormModal>
        </div>
      </CollapsibleCard>
      ) : null}
        </>
      ) : null}

      {activeTab === "expenses" ? (
      <CollapsibleCard
        title="Cash and Expenses"
        summary="Use one focused workflow at a time: IOU advance requests, petty cash vouchers, or out-of-pocket reimbursement claims."
        defaultOpen
      >
        <div className="mb-4 flex flex-wrap gap-2">
          {expenseViews.map((view) => (
            <Link
              key={view.key}
              href={expenseHref(job.id, view.key)}
              className={[
                "rounded-md border px-3 py-1.5 text-sm font-medium transition",
                activeExpenseView === view.key
                  ? "border-[var(--link)] bg-[var(--surface-soft)] text-[var(--foreground)]"
                  : "border-[var(--card-border)] text-zinc-500 hover:border-[var(--link)] hover:text-[var(--foreground)]",
              ].join(" ")}
            >
              {view.label}
            </Link>
          ))}
        </div>

        {activeExpenseView === "ious" ? (
        <div className="rounded-lg border border-[var(--card-border)] p-3">
          <div className="mb-3 flex flex-wrap items-start justify-between gap-2">
            <div>
              <div className="text-sm font-semibold">IOU / Employee Advance</div>
              <div className="mt-1 text-xs text-zinc-500">
                Request an advance against this job and daily sheet. The requester is the signed-in system user; once submitted, the request stays visible in the register below.
              </div>
            </div>
            <JobFormModal title="Request IOU / Employee Advance" description="Create and submit an advance request against this job." buttonLabel="+ Request IOU" variant="secondary" disabled={!canAddJobActivity}>
              <ServiceJobDailyIouCreateForm serviceJobId={job.id} dailySheets={dailySheets} disabled={!canAddJobActivity} />
            </JobFormModal>
          </div>
          <div className="mt-5 border-t border-[var(--card-border)] pt-4">
            <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
              <div>
                <div className="text-sm font-semibold">Job IOU Register</div>
                <div className="mt-1 text-xs text-zinc-500">Cash advances requested against this job remain visible here after submission.</div>
              </div>
              <Link className="text-xs font-semibold text-[var(--link)] underline underline-offset-2" href="/finance/petty-cash-ious">
                Open finance IOUs
              </Link>
            </div>
            <div className="overflow-auto">
              <Table>
                <thead>
                  <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                    <th className="py-2 pr-3">IOU</th>
                    <th className="py-2 pr-3">Daily Sheet</th>
                    <th className="py-2 pr-3">Requester</th>
                    <th className="py-2 pr-3 text-right">Amount</th>
                    <th className="py-2 pr-3">Status</th>
                    <th className="py-2 pr-3">Timeline</th>
                    <th className="py-2 pr-3">Purpose</th>
                  </tr>
                </thead>
                <tbody>
                  {pettyCashIous.map((iou) => (
                    <tr key={iou.id} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                      <td className="py-2 pr-3 font-mono text-xs">{iou.number}</td>
                      <td className="py-2 pr-3 font-mono text-xs">
                        {iou.serviceJobDailySheetId ? dailySheetNumberById.get(iou.serviceJobDailySheetId) ?? "Linked" : "Unlinked"}
                      </td>
                      <td className="py-2 pr-3">{iou.requestedByName}</td>
                      <td className="py-2 pr-3 text-right">{money(iou.amount)}</td>
                      <td className="py-2 pr-3">
                        {pettyCashIouStatusLabel[iou.status] ?? iou.status}
                        {iou.rejectionReason ? <div className="mt-1 text-xs text-red-600 dark:text-red-300">{iou.rejectionReason}</div> : null}
                      </td>
                      <td className="py-2 pr-3 text-xs text-zinc-500">
                        <div>Requested {new Date(iou.requestedAt).toLocaleDateString()}</div>
                        {iou.submittedAt ? <div>Submitted {new Date(iou.submittedAt).toLocaleDateString()}</div> : null}
                        {iou.approvedAt ? <div>Approved {new Date(iou.approvedAt).toLocaleDateString()}</div> : null}
                        {iou.releasedAt ? <div>Released {new Date(iou.releasedAt).toLocaleDateString()}</div> : null}
                        {iou.settledAt ? <div>Settled {new Date(iou.settledAt).toLocaleDateString()}</div> : null}
                      </td>
                      <td className="max-w-sm py-2 pr-3 text-zinc-500">{iou.purpose}</td>
                    </tr>
                  ))}
                  {pettyCashIous.length === 0 ? (
                    <tr>
                      <td className="py-6 text-sm text-zinc-500" colSpan={7}>
                        No IOU advances requested for this job yet.
                      </td>
                    </tr>
                  ) : null}
                </tbody>
              </Table>
            </div>
          </div>
        </div>
        ) : null}

        {activeExpenseView === "petty-cash" ? (
        <div className="rounded-lg border border-[var(--card-border)] p-3">
          <div className="mb-3 flex flex-wrap items-start justify-between gap-2">
            <div>
              <div className="text-sm font-semibold">Petty Cash Expense</div>
              <div className="mt-1 text-xs text-zinc-500">
                Record expenses paid from company petty cash. Capture the accountant-issued bill number and whether the receiver got cash by handover, bank deposit, or another method.
              </div>
            </div>
            <JobFormModal title="Create Petty Cash Voucher" description="Create a petty-cash-funded service expense claim for this job." buttonLabel="+ Petty Cash Voucher" variant="secondary" disabled={!canAddJobActivity}>
              <ServiceJobDailyExpenseClaimCreateForm
                serviceJobId={job.id}
                dailySheets={dailySheets}
                defaultFundingSource="2"
                lockFundingSource
                submitLabel="Create Petty Cash Voucher"
                disabled={!canAddJobActivity}
              />
            </JobFormModal>
          </div>
          <ServiceJobExpenseClaimRegister
            claims={expenseClaims.filter((claim) => claim.fundingSource === 2)}
            dailySheetNumberById={dailySheetNumberById}
            emptyMessage="No petty-cash expense vouchers created for this job yet."
          />
        </div>
        ) : null}

        {activeExpenseView === "reimbursements" ? (
        <div className="rounded-lg border border-[var(--card-border)] p-3">
          <div className="mb-3 flex flex-wrap items-start justify-between gap-2">
            <div>
              <div className="text-sm font-semibold">Employee Out-of-Pocket Claim</div>
              <div className="mt-1 text-xs text-zinc-500">
                Capture expenses paid personally by the signed-in user. Create the claim, add lines on the claim detail page, then submit for approval and reimbursement settlement.
              </div>
            </div>
            <JobFormModal title="Create Reimbursement Claim" description="Create an out-of-pocket reimbursement claim for this job." buttonLabel="+ Reimbursement Claim" variant="secondary" disabled={!canAddJobActivity}>
              <ServiceJobDailyExpenseClaimCreateForm
                serviceJobId={job.id}
                dailySheets={dailySheets}
                defaultFundingSource="1"
                lockFundingSource
                submitLabel="Create Reimbursement Claim"
                disabled={!canAddJobActivity}
              />
            </JobFormModal>
          </div>
          <ServiceJobExpenseClaimRegister
            claims={expenseClaims.filter((claim) => claim.fundingSource === 1)}
            dailySheetNumberById={dailySheetNumberById}
            emptyMessage="No out-of-pocket reimbursement claims created for this job yet."
          />
        </div>
        ) : null}
      </CollapsibleCard>
      ) : null}

      {activeTab === "materials" ? (
        <>
      <div className="flex flex-wrap gap-2">
        {materialViews.map((view) => (
          <Link
            key={view.key}
            href={materialHref(job.id, view.key)}
            className={[
              "rounded-md border px-3 py-1.5 text-sm font-medium transition",
              activeMaterialView === view.key
                ? "border-[var(--link)] bg-[var(--surface-soft)] text-[var(--foreground)]"
                : "border-[var(--card-border)] text-zinc-500 hover:border-[var(--link)] hover:text-[var(--foreground)]",
            ].join(" ")}
          >
            {view.label}
          </Link>
        ))}
      </div>

      {activeMaterialView === "issues" ? (
        <Card>
          <div className="mb-3">
            <div className="text-sm font-semibold">Issued MRNs</div>
            <div className="mt-1 text-xs text-zinc-500">Posted material requisitions issued to this job. Expand an MRN to see the issued item lines.</div>
          </div>
          <div className="overflow-auto">
            <Table>
              <thead>
                <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                  <th className="py-2 pr-3">MRN</th>
                  <th className="py-2 pr-3">Daily Sheet</th>
                  <th className="py-2 pr-3">Issued</th>
                  <th className="py-2 pr-3">Warehouse</th>
                  <th className="py-2 pr-3 text-right">Items</th>
                  <th className="py-2 pr-3 text-right">Qty</th>
                  <th className="py-2 pr-3 text-right">Cost</th>
                </tr>
              </thead>
              <tbody>
                {issuedMaterialGroups.map((group) => (
                  <tr key={group.id} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                    <td className="py-2 pr-3" colSpan={7}>
                      <details>
                        <summary className="grid cursor-pointer list-none gap-3 rounded-md px-2 py-1 hover:bg-[var(--surface-soft)] md:grid-cols-[1.2fr_1fr_1fr_0.8fr_0.5fr_0.5fr_0.6fr]">
                          <span className="font-mono text-xs">
                            <TransactionLink referenceType="MR" referenceId={group.id} monospace>
                              {group.number}
                            </TransactionLink>
                          </span>
                          <span>{group.dailySheetNumber ?? (group.dailySheetId ? dailySheetNumberById.get(group.dailySheetId) : null) ?? "Unlinked"}</span>
                          <span className="text-zinc-500">{new Date(group.occurredAt).toLocaleString()}</span>
                          <span>{group.warehouseCode}</span>
                          <span className="text-right">{group.lines.length}</span>
                          <span className="text-right">{group.quantity}</span>
                          <span className="text-right">{money(group.total)}</span>
                        </summary>
                        <div className="mt-2 overflow-auto rounded-md border border-[var(--card-border)]">
                          <Table>
                            <thead>
                              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                                <th className="py-2 pl-3 pr-3">Item</th>
                                <th className="py-2 pr-3 text-right">Qty</th>
                                <th className="py-2 pr-3 text-right">Unit Cost</th>
                                <th className="py-2 pr-3 text-right">Line Total</th>
                              </tr>
                            </thead>
                            <tbody>
                              {group.lines.map((line) => (
                                <tr key={line.materialRequisitionLineId} className="border-b border-zinc-100 dark:border-zinc-900">
                                  <td className="py-2 pl-3 pr-3">
                                    <ItemInlineLink itemId={line.itemId}>{line.itemSku} - {line.itemName}</ItemInlineLink>
                                  </td>
                                  <td className="py-2 pr-3 text-right">{line.quantity}</td>
                                  <td className="py-2 pr-3 text-right">{money(line.unitCost)}</td>
                                  <td className="py-2 pr-3 text-right">{money(line.lineTotal)}</td>
                                </tr>
                              ))}
                            </tbody>
                          </Table>
                        </div>
                      </details>
                    </td>
                  </tr>
                ))}
                {issuedMaterialGroups.length === 0 ? (
                  <tr>
                    <td className="py-6 text-sm text-zinc-500" colSpan={7}>
                      No posted MRNs issued to this job.
                    </td>
                  </tr>
                ) : null}
              </tbody>
            </Table>
          </div>
        </Card>
      ) : null}

      {activeMaterialView === "issues" ? (
        <Card>
          <div className="flex items-center justify-between gap-3">
            <div>
              <div className="text-sm font-semibold">Create MRN</div>
              <div className="mt-0.5 text-xs text-zinc-500">Create a draft material requisition, then add lines and post it from the MRN screen.</div>
            </div>
            <JobFormModal title="Create MRN" description="Create a draft material requisition for this job." buttonLabel="+ New MRN" variant="secondary" disabled={!canAddJobActivity}>
            <ServiceJobDailyMaterialRequisitionCreateForm serviceJobId={job.id} dailySheets={dailySheets} warehouses={warehouses} disabled={!canAddJobActivity} />
            </JobFormModal>
          </div>
        </Card>
      ) : null}

      {activeMaterialView === "returns" ? (
      <CollapsibleCard
        title="Return Materials"
        summary="Draft not-needed, wrongly-issued, or supplier-rejected returns first, then post to receive usable stock."
        defaultOpen
      >
        <div className="mb-3 flex justify-end">
          <JobFormModal title="Save Material Return Draft" description="Draft not-needed, wrongly-issued, or supplier-rejected returns before posting usable stock back." buttonLabel="+ Material Return" variant="secondary" disabled={!canAddJobActivity}>
            <ServiceJobMaterialDispositionAddForm
              serviceJobId={job.id}
              materialLines={costing.materialLines}
              dailySheets={dailySheets}
              allowedKinds={["1", "2", "4"]}
              submitLabel="Save Material Return Draft"
              disabled={!canAddJobActivity}
            />
          </JobFormModal>
        </div>
        <div className="overflow-auto">
          <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
            <div className="text-sm font-medium">Return Material Drafts / Posted</div>
            <div className="text-xs text-zinc-500">{returnMaterialDispositions.filter((disposition) => !disposition.isVoided).length} active / {returnMaterialDispositions.length} total</div>
          </div>
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Date</th>
                <th className="py-2 pr-3">Daily Sheet</th>
                <th className="py-2 pr-3">MRN Line</th>
                <th className="py-2 pr-3">Disposition</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Qty</th>
                <th className="py-2 pr-3">Stock Effect</th>
                <th className="py-2 pr-3">Charge To</th>
                <th className="py-2 pr-3">Condition / Reason</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {returnMaterialDispositions.map((disposition) => {
                const line = materialLineById.get(disposition.materialRequisitionLineId);
                return (
                  <tr key={disposition.id} className={`border-b border-zinc-100 align-top dark:border-zinc-900 ${disposition.isVoided ? "opacity-60" : ""}`}>
                    <td className="py-2 pr-3 whitespace-nowrap">{new Date(disposition.createdAt).toLocaleString()}</td>
                    <td className="py-2 pr-3 font-mono text-xs">
                      {disposition.serviceJobDailySheetId ? dailySheetNumberById.get(disposition.serviceJobDailySheetId) ?? "Linked" : "Unlinked"}
                    </td>
                    <td className="py-2 pr-3">
                      {line ? (
                        <div>
                          <TransactionLink referenceType="MR" referenceId={line.materialRequisitionId} monospace>
                            {line.materialRequisitionNumber}
                          </TransactionLink>
                          <div className="text-xs text-zinc-500">
                            <ItemInlineLink itemId={line.itemId}>{line.itemSku} - {line.itemName}</ItemInlineLink>
                          </div>
                        </div>
                      ) : (
                        <span className="font-mono text-xs">{disposition.materialRequisitionLineId.slice(0, 8)}</span>
                      )}
                    </td>
                    <td className="py-2 pr-3">{materialDispositionLabel[disposition.kind] ?? disposition.kind}</td>
                    <td className="py-2 pr-3">
                      {disposition.status === 0 ? "Draft" : disposition.status === 1 ? "Posted" : "Voided"}
                      {disposition.postedAt ? <div className="mt-1 text-xs text-zinc-500">{new Date(disposition.postedAt).toLocaleString()}</div> : null}
                    </td>
                    <td className="py-2 pr-3">{disposition.quantity}</td>
                    <td className="py-2 pr-3">{materialStockEffectLabel[disposition.kind] ?? "-"}</td>
                    <td className="py-2 pr-3">{materialChargeToLabel[disposition.chargeTo] ?? disposition.chargeTo}</td>
                    <td className="py-2 pr-3 text-xs text-zinc-500">
                      <div>{maybeText(disposition.condition)}</div>
                      <div className="mt-1 whitespace-pre-wrap">{disposition.reason}</div>
                      {disposition.supplierReturnId ? <div className="mt-1">Supplier return: {disposition.supplierReturnId}</div> : null}
                      {disposition.responsiblePerson ? <div className="mt-1">Responsible: {disposition.responsiblePerson}</div> : null}
                      {disposition.serials.length > 0 ? <div className="mt-1">Serials: {disposition.serials.join(", ")}</div> : null}
                    </td>
                    <td className="py-2 pr-3">
                      <ServiceJobMaterialDispositionActions serviceJobId={job.id} dispositionId={disposition.id} disposition={disposition} disabled={!canAddJobActivity} />
                    </td>
                  </tr>
                );
              })}
              {returnMaterialDispositions.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={10}>
                    No return material drafts yet.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </CollapsibleCard>
      ) : null}

      {activeMaterialView === "damage" ? (
      <CollapsibleCard
        title="Damage Material"
        summary="Record issued material that is damaged or unusable. Posting a damage draft does not receive usable stock."
        defaultOpen
      >
        <div className="mb-3 flex justify-end">
          <JobFormModal title="Save Damage Draft" description="Record issued material that is damaged or unusable." buttonLabel="+ Damage Draft" variant="secondary" disabled={!canAddJobActivity}>
            <ServiceJobMaterialDispositionAddForm
              serviceJobId={job.id}
              materialLines={costing.materialLines}
              dailySheets={dailySheets}
              allowedKinds={["3"]}
              submitLabel="Save Damage Draft"
              disabled={!canAddJobActivity}
            />
          </JobFormModal>
        </div>
        <div className="overflow-auto">
          <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
            <div className="text-sm font-medium">Damage Drafts / Posted</div>
            <div className="text-xs text-zinc-500">{damageMaterialDispositions.filter((disposition) => !disposition.isVoided).length} active / {damageMaterialDispositions.length} total</div>
          </div>
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Date</th>
                <th className="py-2 pr-3">Daily Sheet</th>
                <th className="py-2 pr-3">MRN Line</th>
                <th className="py-2 pr-3">Disposition</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Qty</th>
                <th className="py-2 pr-3">Stock Effect</th>
                <th className="py-2 pr-3">Charge To</th>
                <th className="py-2 pr-3">Condition / Reason</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {damageMaterialDispositions.map((disposition) => {
                const line = materialLineById.get(disposition.materialRequisitionLineId);
                return (
                  <tr key={disposition.id} className={`border-b border-zinc-100 align-top dark:border-zinc-900 ${disposition.isVoided ? "opacity-60" : ""}`}>
                    <td className="py-2 pr-3 whitespace-nowrap">{new Date(disposition.createdAt).toLocaleString()}</td>
                    <td className="py-2 pr-3 font-mono text-xs">
                      {disposition.serviceJobDailySheetId ? dailySheetNumberById.get(disposition.serviceJobDailySheetId) ?? "Linked" : "Unlinked"}
                    </td>
                    <td className="py-2 pr-3">
                      {line ? (
                        <div>
                          <TransactionLink referenceType="MR" referenceId={line.materialRequisitionId} monospace>
                            {line.materialRequisitionNumber}
                          </TransactionLink>
                          <div className="text-xs text-zinc-500">
                            <ItemInlineLink itemId={line.itemId}>{line.itemSku} - {line.itemName}</ItemInlineLink>
                          </div>
                        </div>
                      ) : (
                        <span className="font-mono text-xs">{disposition.materialRequisitionLineId.slice(0, 8)}</span>
                      )}
                    </td>
                    <td className="py-2 pr-3">
                      {materialDispositionLabel[disposition.kind] ?? disposition.kind}
                      {disposition.isVoided ? <div className="mt-1 text-xs font-medium text-red-600 dark:text-red-300">Voided</div> : null}
                    </td>
                    <td className="py-2 pr-3">
                      {disposition.status === 0 ? "Draft" : disposition.status === 1 ? "Posted" : "Voided"}
                      {disposition.postedAt ? <div className="mt-1 text-xs text-zinc-500">{new Date(disposition.postedAt).toLocaleString()}</div> : null}
                    </td>
                    <td className="py-2 pr-3">{disposition.quantity}</td>
                    <td className="py-2 pr-3">{materialStockEffectLabel[disposition.kind] ?? "-"}</td>
                    <td className="py-2 pr-3">{materialChargeToLabel[disposition.chargeTo] ?? disposition.chargeTo}</td>
                    <td className="py-2 pr-3 text-xs text-zinc-500">
                      <div>{maybeText(disposition.condition)}</div>
                      <div className="mt-1 whitespace-pre-wrap">{disposition.reason}</div>
                      {disposition.supplierReturnId ? <div className="mt-1">Supplier return: {disposition.supplierReturnId}</div> : null}
                      {disposition.responsiblePerson ? <div className="mt-1">Responsible: {disposition.responsiblePerson}</div> : null}
                      {disposition.serials.length > 0 ? <div className="mt-1">Serials: {disposition.serials.join(", ")}</div> : null}
                      {disposition.isVoided ? (
                        <div className="mt-1 text-red-600 dark:text-red-300">
                          Voided {disposition.voidedAt ? new Date(disposition.voidedAt).toLocaleString() : ""}: {maybeText(disposition.voidReason)}
                        </div>
                      ) : null}
                    </td>
                    <td className="py-2 pr-3">
                      <ServiceJobMaterialDispositionActions
                        serviceJobId={job.id}
                        dispositionId={disposition.id}
                        disposition={disposition}
                        disabled={!canAddJobActivity}
                      />
                    </td>
                  </tr>
                );
              })}
              {damageMaterialDispositions.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={10}>
                    No damage material drafts yet.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </CollapsibleCard>
      ) : null}
        </>
      ) : null}

      {activeTab === "costs" ? (
        <>
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
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div>
            <div className="text-sm font-semibold">Actual Cost Breakdown</div>
            <div className="mt-1 text-xs text-zinc-500">Only posted/approved actuals are included in the actual cost total.</div>
          </div>
          <div className="text-right">
            <div className="text-xs uppercase tracking-wide text-zinc-500">Total</div>
            <div className="text-lg font-semibold">{money(costing.totalActualCost)}</div>
          </div>
        </div>
        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
          {actualCostBreakdown.map((line) => (
            <a
              key={line.label}
              href={line.href}
              className="rounded-md border border-[var(--card-border)] p-3 transition hover:border-[var(--link)] hover:bg-[var(--surface-soft)]"
            >
              <div className="text-xs uppercase tracking-wide text-zinc-500">{line.label}</div>
              <div className="mt-2 text-xl font-semibold">{money(line.amount)}</div>
              <div className="mt-1 text-xs text-zinc-500">{line.detail}</div>
            </a>
          ))}
        </div>
      </Card>

      <CollapsibleCard title="Profitability Report" summary="Detailed cost, margin, estimate, and invoice totals.">
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
      </CollapsibleCard>
        </>
      ) : null}

      {activeTab === "billing" ? (
      <CollapsibleCard
        title="Warranty / Billing Entitlement"
        summary={job.entitlementSummary ?? "Coverage and billing treatment calculated when the job was opened."}
        meta={billingTreatmentLabel[job.customerBillingTreatment] ?? String(job.customerBillingTreatment)}
      >
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <div>
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
          </div>
          <div>
            <div className="text-xs uppercase tracking-wide text-zinc-500">Coverage</div>
            <div className="mt-2 text-sm font-medium">{entitlementCoverageLabel[job.entitlementCoverage] ?? job.entitlementCoverage}</div>
          </div>
          <div>
            <div className="text-xs uppercase tracking-wide text-zinc-500">Billing Treatment</div>
            <div className="mt-2 text-sm font-medium">{billingTreatmentLabel[job.customerBillingTreatment] ?? job.customerBillingTreatment}</div>
          </div>
          <div>
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
          </div>
        </div>
      </CollapsibleCard>
      ) : null}

      {activeTab === "billing" ? (
      <CollapsibleCard
        title="Quotations & Final Invoices"
        summary="Linked service estimates and final sales invoices."
        meta={`${costing.estimates.length} estimates / ${costing.invoices.length} invoices`}
      >
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
      </CollapsibleCard>
      ) : null}

      {activeTab === "costs" ? (
      <CollapsibleCard title="Cost Sources" summary="Posted material, direct purchase, labor, and petty cash source lines.">
        <div className="space-y-4">
          <div className="overflow-auto">
            <div id="job-cost-materials" className="mb-2 scroll-mt-24 text-sm font-medium">Material Consumption</div>
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
                  const dispositionsForLine = materialDispositions.filter((disposition) => disposition.materialRequisitionLineId === line.materialRequisitionLineId && !disposition.isVoided);
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
            <div id="job-cost-direct-purchases" className="mb-2 scroll-mt-24 text-sm font-medium">Direct Purchases</div>
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
            <div id="job-cost-labor" className="mb-2 scroll-mt-24 text-sm font-medium">Work-Order Labor Entries</div>
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
                      <TransactionLink referenceType="WO" referenceId={line.workOrderId}>
                        Job Sheet
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
            <div id="job-cost-petty-cash" className="mb-2 scroll-mt-24 text-sm font-medium">Petty Cash</div>
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
      </CollapsibleCard>
      ) : null}

      {activeTab === "files" ? (
      <CollapsibleCard title="Comments & Attachments" summary="Notes, approvals, customer communication, and uploaded files.">
        <DocumentCollaborationPanel referenceType="SJ" referenceId={id} />
      </CollapsibleCard>
      ) : null}
    </div>
  );
}
