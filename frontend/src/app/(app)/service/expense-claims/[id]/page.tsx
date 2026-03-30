import Link from "next/link";
import { cookies } from "next/headers";
import { backendFetchJson } from "@/lib/backend.server";
import { ISS_TOKEN_COOKIE } from "@/lib/env";
import { sessionFromToken } from "@/lib/jwt";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";
import { Card, SecondaryLink, Table } from "@/components/ui";
import { TransactionLink } from "@/components/TransactionLink";
import { ServiceExpenseClaimActions } from "../ServiceExpenseClaimActions";
import { ServiceExpenseClaimConvertEstimateForm } from "../ServiceExpenseClaimConvertEstimateForm";
import { ServiceExpenseClaimLineAddForm } from "../ServiceExpenseClaimLineAddForm";
import { ServiceExpenseClaimLineRow } from "../ServiceExpenseClaimLineRow";

type ServiceExpenseClaimDto = {
  id: string;
  number: string;
  serviceJobId: string;
  claimedByUserId?: string | null;
  claimedByName: string;
  fundingSource: number;
  expenseDate: string;
  merchantName?: string | null;
  receiptReference?: string | null;
  notes?: string | null;
  status: number;
  submittedAt?: string | null;
  approvedAt?: string | null;
  rejectedAt?: string | null;
  rejectionReason?: string | null;
  settlementPaymentTypeId?: string | null;
  settlementPettyCashFundId?: string | null;
  settledAt?: string | null;
  settlementReference?: string | null;
  total: number;
  billableUnconvertedLineCount: number;
  lines: {
    id: string;
    itemId?: string | null;
    description: string;
    quantity: number;
    unitCost: number;
    billableToCustomer: boolean;
    convertedToServiceEstimateId?: string | null;
    convertedToServiceEstimateLineId?: string | null;
    convertedToEstimateAt?: string | null;
    lineTotal: number;
  }[];
};

type ServiceJobDto = { id: string; number: string };
type ItemDto = { id: string; sku: string; name: string };
type PaymentTypeDto = { id: string; code: string; name: string; isActive: boolean };
type PettyCashFundDto = { id: string; code: string; name: string; balance: number; isActive: boolean };
type ServiceEstimateSummaryDto = { id: string; number: string; revisionNumber: number; status: number; total: number; serviceJobId: string };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Submitted",
  2: "Approved",
  3: "Rejected",
  4: "Settled",
};

const fundingSourceLabel: Record<number, string> = {
  1: "Out of Pocket",
  2: "Petty Cash",
};

export default async function ServiceExpenseClaimDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const cookieStore = await cookies();
  const token = cookieStore.get(ISS_TOKEN_COOKIE)?.value;
  const session = token ? sessionFromToken(token) : null;
  const roles = new Set(session?.roles ?? []);
  const isFinanceOrAdmin = roles.has("Admin") || roles.has("Finance");

  const [claim, jobs, items, paymentTypes, pettyCashFunds, estimates] = await Promise.all([
    backendFetchJson<ServiceExpenseClaimDto>(`/service/expense-claims/${id}`),
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=500"),
    backendFetchJson<ItemDto[]>("/items"),
    isFinanceOrAdmin ? backendFetchJson<PaymentTypeDto[]>("/payment-types") : Promise.resolve([]),
    isFinanceOrAdmin ? backendFetchJson<PettyCashFundDto[]>("/finance/petty-cash-funds") : Promise.resolve([]),
    backendFetchJson<ServiceEstimateSummaryDto[]>("/service/estimates?take=500"),
  ]);

  const jobById = new Map(jobs.map((job) => [job.id, job]));
  const paymentTypeById = new Map(paymentTypes.map((paymentType) => [paymentType.id, paymentType]));
  const pettyCashFundById = new Map(pettyCashFunds.map((fund) => [fund.id, fund]));
  const isDraft = claim.status === 0;
  const canSubmit = claim.status === 0;
  const canApprove = claim.status === 1 && isFinanceOrAdmin;
  const canReject = claim.status === 1 && isFinanceOrAdmin;
  const canSettle = claim.status === 2 && isFinanceOrAdmin;
  const canConvertBillable = !isDraft && claim.billableUnconvertedLineCount > 0 && (claim.status === 2 || claim.status === 4);
  const jobEstimates = estimates.filter((estimate) => estimate.serviceJobId === claim.serviceJobId);

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/service/expense-claims" className="hover:underline">
            Expense Claims
          </Link>{" "}
          / <span className="font-mono text-xs">{claim.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Expense Claim {claim.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>
            Job:{" "}
            <TransactionLink referenceType="SJ" referenceId={claim.serviceJobId} monospace>
              {jobById.get(claim.serviceJobId)?.number ?? claim.serviceJobId}
            </TransactionLink>
          </div>
          <div>Claimed by: {claim.claimedByName}</div>
          <div>Funding: {fundingSourceLabel[claim.fundingSource] ?? claim.fundingSource}</div>
          <div>Status: {statusLabel[claim.status] ?? claim.status}</div>
          <div>Expense date: {new Date(claim.expenseDate).toLocaleString()}</div>
          <div>Total: {claim.total.toFixed(2)}</div>
        </div>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-500">
          {claim.merchantName ? <div>Merchant: {claim.merchantName}</div> : null}
          {claim.receiptReference ? <div>Receipt ref: {claim.receiptReference}</div> : null}
          {claim.submittedAt ? <div>Submitted: {new Date(claim.submittedAt).toLocaleString()}</div> : null}
          {claim.approvedAt ? <div>Approved: {new Date(claim.approvedAt).toLocaleString()}</div> : null}
          {claim.rejectedAt ? <div>Rejected: {new Date(claim.rejectedAt).toLocaleString()}</div> : null}
          {claim.settledAt ? <div>Settled: {new Date(claim.settledAt).toLocaleString()}</div> : null}
          {claim.settlementPaymentTypeId ? (
            <div>
              Method: {paymentTypeById.get(claim.settlementPaymentTypeId)?.code ?? claim.settlementPaymentTypeId}
            </div>
          ) : null}
          {claim.settlementPettyCashFundId ? (
            <div>
              Fund:{" "}
              <Link className="underline" href={`/finance/petty-cash/${claim.settlementPettyCashFundId}`}>
                {pettyCashFundById.get(claim.settlementPettyCashFundId)?.code ?? claim.settlementPettyCashFundId}
              </Link>
            </div>
          ) : null}
        </div>
        {claim.settlementReference ? <div className="mt-2 text-sm text-zinc-500">Settlement ref: {claim.settlementReference}</div> : null}
      </div>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink
            href={`/api/backend/service/expense-claims/${claim.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
        <ServiceExpenseClaimActions
          claimId={claim.id}
          fundingSource={claim.fundingSource}
          paymentTypes={paymentTypes.filter((paymentType) => paymentType.isActive)}
          pettyCashFunds={pettyCashFunds}
          canSubmit={canSubmit}
          canApprove={canApprove}
          canReject={canReject}
          canSettle={canSettle}
        />
      </Card>

      {canConvertBillable ? (
        <Card>
          <ServiceExpenseClaimConvertEstimateForm claimId={claim.id} estimates={jobEstimates} disabled={false} />
        </Card>
      ) : null}

      {claim.notes ? (
        <Card>
          <div className="mb-2 text-sm font-semibold">Notes</div>
          <div className="whitespace-pre-wrap text-sm text-zinc-700 dark:text-zinc-200">{claim.notes}</div>
        </Card>
      ) : null}

      {claim.rejectionReason ? (
        <Card>
          <div className="mb-2 text-sm font-semibold">Rejection Reason</div>
          <div className="whitespace-pre-wrap text-sm text-zinc-700 dark:text-zinc-200">{claim.rejectionReason}</div>
        </Card>
      ) : null}

      {isDraft ? (
        <Card>
          <div className="mb-3 text-sm font-semibold">Add line</div>
          <ServiceExpenseClaimLineAddForm claimId={claim.id} items={items} />
        </Card>
      ) : null}

      <Card>
        <div className="mb-3 text-sm font-semibold">Lines</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Item</th>
                <th className="py-2 pr-3">Description</th>
                <th className="py-2 pr-3">Qty</th>
                <th className="py-2 pr-3">Unit Cost</th>
                <th className="py-2 pr-3">Billable</th>
                <th className="py-2 pr-3">Estimate</th>
                <th className="py-2 pr-3">Line Total</th>
                {isDraft ? <th className="py-2 pr-3">Actions</th> : null}
              </tr>
            </thead>
            <tbody>
              {claim.lines.map((line) =>
                isDraft ? (
                  <ServiceExpenseClaimLineRow
                    key={line.id}
                    claimId={claim.id}
                    line={line}
                    items={items}
                    canEdit={isDraft}
                  />
                ) : (
                  <tr key={line.id} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                    <td className="py-2 pr-3 text-zinc-500">
                      {line.itemId ? `${items.find((item) => item.id === line.itemId)?.sku ?? line.itemId}` : "-"}
                    </td>
                    <td className="py-2 pr-3 text-zinc-500">{line.description}</td>
                    <td className="py-2 pr-3">{line.quantity}</td>
                    <td className="py-2 pr-3">{line.unitCost.toFixed(2)}</td>
                    <td className="py-2 pr-3">{line.billableToCustomer ? "Yes" : "No"}</td>
                    <td className="py-2 pr-3 text-zinc-500">
                      {line.convertedToServiceEstimateId ? (
                        <TransactionLink referenceType="SE" referenceId={line.convertedToServiceEstimateId}>
                          {jobEstimates.find((estimate) => estimate.id === line.convertedToServiceEstimateId)?.number ?? "Open estimate"}
                        </TransactionLink>
                      ) : (
                        "-"
                      )}
                    </td>
                    <td className="py-2 pr-3">{line.lineTotal.toFixed(2)}</td>
                  </tr>
                ),
              )}
              {claim.lines.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={isDraft ? 8 : 7}>
                    No lines yet.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>

      {!isDraft && claim.lines.some((line) => line.billableToCustomer) ? (
        <Card>
          <div className="text-sm text-zinc-500">
            Billable lines on this claim should be pushed into the working estimate or estimate revision before customer approval and invoice conversion.
          </div>
        </Card>
      ) : null}

      <DocumentCollaborationPanel referenceType="SEC" referenceId={id} title="Expense Claim Comments & Attachments" />
    </div>
  );
}
