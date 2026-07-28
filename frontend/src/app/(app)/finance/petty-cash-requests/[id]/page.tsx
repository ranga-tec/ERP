import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";
import { TransactionLink } from "@/components/TransactionLink";
import { Card, Table } from "@/components/ui";
import { PettyCashRequestActions } from "../PettyCashRequestActions";
import { PettyCashRequestFundLineForm } from "../PettyCashRequestFundLineForm";
import { PettyCashRequestLineAddForm } from "../PettyCashRequestLineAddForm";
import {
  STATUS_APPROVED,
  STATUS_DRAFT,
  STATUS_PARTIALLY_FUNDED,
  describeCategory,
  money,
  statusLabel,
} from "../categories";

type FundingDto = {
  id: string;
  amount: number;
  fundedAt: string;
  paymentReference?: string | null;
  notes?: string | null;
};

type LineDto = {
  id: string;
  category: number;
  serviceJobId?: string | null;
  serviceJobNumber?: string | null;
  customCategoryName?: string | null;
  purpose: string;
  requestedAmount: number;
  approvedAmount?: number | null;
  fundedAmount: number;
  outstandingAmount: number;
  subAccountBalance: number;
  fundings: FundingDto[];
};

type PettyCashRequestDto = {
  id: string;
  number: string;
  pettyCashFundId: string;
  pettyCashFundCode?: string | null;
  requestedByName: string;
  requestedAt: string;
  neededByAt?: string | null;
  notes?: string | null;
  status: number;
  submittedAt?: string | null;
  approvedAt?: string | null;
  rejectedAt?: string | null;
  rejectionReason?: string | null;
  requestedTotal: number;
  approvedTotal: number;
  fundedTotal: number;
  outstandingTotal: number;
  lines: LineDto[];
};

type ServiceJobDto = { id: string; number: string };
type CurrentUserPermissionsDto = { permissions: string[] };

export default async function PettyCashRequestDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [request, jobs, currentPermissions] = await Promise.all([
    backendFetchJson<PettyCashRequestDto>(`/finance/petty-cash-requests/${id}`),
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=500"),
    backendFetchJson<CurrentUserPermissionsDto>("/me/permissions"),
  ]);

  const permissions = new Set(currentPermissions.permissions);
  const isDraft = request.status === STATUS_DRAFT;
  const canEdit = isDraft && permissions.has("Finance.PettyCashRequest.Edit");
  const canFund =
    (request.status === STATUS_APPROVED || request.status === STATUS_PARTIALLY_FUNDED)
    && permissions.has("Finance.PettyCashRequest.Fund");

  // One transfer usually covers several lines, so the first reference already entered is offered
  // as the default for the rest.
  const existingPaymentReference = request.lines
    .flatMap((line) => line.fundings)
    .map((funding) => funding.paymentReference)
    .find((reference): reference is string => Boolean(reference));

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/finance/petty-cash-requests" className="hover:underline">
            Petty Cash Requests
          </Link>{" "}
          / <span className="font-mono text-xs">{request.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Petty Cash Request {request.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>
            Fund:{" "}
            {request.pettyCashFundCode ? (
              <Link className="underline" href={`/finance/petty-cash/${request.pettyCashFundId}`}>
                {request.pettyCashFundCode}
              </Link>
            ) : (
              <span className="text-zinc-400">Fund removed</span>
            )}
          </div>
          <div>Requested by: {request.requestedByName}</div>
          <div>Status: {statusLabel[request.status] ?? request.status}</div>
          <div>Date: {new Date(request.requestedAt).toLocaleDateString()}</div>
          {request.neededByAt ? <div>Needed by: {new Date(request.neededByAt).toLocaleDateString()}</div> : null}
        </div>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-500">
          <div>Requested: {money(request.requestedTotal)}</div>
          <div>Approved: {money(request.approvedTotal)}</div>
          <div>Funded: {money(request.fundedTotal)}</div>
          {request.outstandingTotal > 0 ? (
            <div className="text-amber-700 dark:text-amber-400">
              Awaiting release: {money(request.outstandingTotal)}
            </div>
          ) : null}
        </div>
        {request.rejectionReason ? (
          <div className="mt-2 text-sm text-red-700 dark:text-red-300">Rejected: {request.rejectionReason}</div>
        ) : null}
        {request.notes ? <div className="mt-2 text-sm text-zinc-500">{request.notes}</div> : null}
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Actions</div>
        <PettyCashRequestActions
          requestId={request.id}
          status={request.status}
          lines={request.lines.map((line) => ({
            id: line.id,
            purpose: line.purpose,
            requestedAmount: line.requestedAmount,
            approvedAmount: line.approvedAmount,
          }))}
          permissions={currentPermissions.permissions}
        />
      </Card>

      <Card>
        <div className="mb-3 flex flex-wrap items-start justify-between gap-3">
          <div>
            <div className="text-sm font-semibold">Category lines</div>
            <div className="mt-1 text-xs text-zinc-500">
              Each category is funded and tracked separately as a sub-account of the fund.
            </div>
          </div>
          {canEdit ? (
            <AppFormModal
              title="Add Category Line"
              description="Add a category to this draft request."
              buttonLabel="+ Add Line"
              variant="secondary"
            >
              <PettyCashRequestLineAddForm requestId={request.id} serviceJobs={jobs} />
            </AppFormModal>
          ) : null}
        </div>

        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Category</th>
                <th className="py-2 pr-3">Purpose</th>
                <th className="py-2 pr-3 text-right">Requested</th>
                <th className="py-2 pr-3 text-right">Approved</th>
                <th className="py-2 pr-3 text-right">Funded</th>
                <th className="py-2 pr-3 text-right">In sub-account</th>
                {canFund ? <th className="py-2 pr-3">Release</th> : null}
              </tr>
            </thead>
            <tbody>
              {request.lines.map((line) => (
                <tr key={line.id} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                  <td className="py-2 pr-3 text-sm">
                    {line.serviceJobId && line.serviceJobNumber ? (
                      <TransactionLink referenceType="SJ" referenceId={line.serviceJobId} monospace>
                        {describeCategory(line.category, line.serviceJobNumber, line.customCategoryName)}
                      </TransactionLink>
                    ) : (
                      describeCategory(line.category, line.serviceJobNumber, line.customCategoryName)
                    )}
                  </td>
                  <td className="max-w-sm py-2 pr-3 text-sm text-zinc-600 dark:text-zinc-400">
                    {line.purpose}
                    {line.fundings.length > 0 ? (
                      <div className="mt-1 space-y-0.5 text-xs text-zinc-500">
                        {line.fundings.map((funding) => (
                          <div key={funding.id}>
                            {money(funding.amount)} on {new Date(funding.fundedAt).toLocaleDateString()}
                            {funding.paymentReference ? ` - ${funding.paymentReference}` : ""}
                          </div>
                        ))}
                      </div>
                    ) : null}
                  </td>
                  <td className="py-2 pr-3 text-right text-sm">{money(line.requestedAmount)}</td>
                  <td className="py-2 pr-3 text-right text-sm">
                    {line.approvedAmount == null ? (
                      <span className="text-zinc-400">-</span>
                    ) : (
                      money(line.approvedAmount)
                    )}
                  </td>
                  <td className="py-2 pr-3 text-right text-sm">
                    {money(line.fundedAmount)}
                    {line.outstandingAmount > 0 ? (
                      <div className="text-xs text-amber-700 dark:text-amber-400">
                        {money(line.outstandingAmount)} to come
                      </div>
                    ) : null}
                  </td>
                  <td className="py-2 pr-3 text-right text-sm">{money(line.subAccountBalance)}</td>
                  {canFund ? (
                    <td className="py-2 pr-3">
                      <PettyCashRequestFundLineForm
                        requestId={request.id}
                        lineId={line.id}
                        outstandingAmount={line.outstandingAmount}
                        defaultPaymentReference={existingPaymentReference}
                      />
                    </td>
                  ) : null}
                </tr>
              ))}
              {request.lines.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={canFund ? 7 : 6}>
                    No category lines yet.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>

      <DocumentCollaborationPanel referenceType="PCR" referenceId={id} title="Request Comments & Attachments" />
    </div>
  );
}
