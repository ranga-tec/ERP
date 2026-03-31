import Link from "next/link";
import { cookies } from "next/headers";
import { backendFetchJson } from "@/lib/backend.server";
import { ISS_TOKEN_COOKIE } from "@/lib/env";
import { sessionFromToken } from "@/lib/jwt";
import { Card, SecondaryLink } from "@/components/ui";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";
import { TransactionLink } from "@/components/TransactionLink";
import { ServiceHandoverActions } from "../ServiceHandoverActions";
import { ServiceHandoverConvertInvoiceForm } from "../ServiceHandoverConvertInvoiceForm";

type ServiceHandoverDto = {
  id: string;
  number: string;
  serviceJobId: string;
  handoverDate: string;
  itemsReturned: string;
  postServiceWarrantyMonths?: number | null;
  customerAcknowledgement?: string | null;
  notes?: string | null;
  status: number;
  salesInvoiceId?: string | null;
  salesInvoiceNumber?: string | null;
  convertedToInvoiceAt?: string | null;
};

type ServiceJobDto = { id: string; number: string; customerId: string; status: number };
type CustomerDto = { id: string; code: string; name: string };
type ServiceEstimateSummaryDto = {
  id: string;
  number: string;
  serviceJobId: string;
  issuedAt: string;
  validUntil?: string | null;
  status: number;
  subtotal: number;
  taxTotal: number;
  total: number;
  lineCount: number;
};
type ItemDto = { id: string; sku: string; name: string };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Completed",
  2: "Cancelled",
};

export default async function ServiceHandoverDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const cookieStore = await cookies();
  const token = cookieStore.get(ISS_TOKEN_COOKIE)?.value;
  const session = token ? sessionFromToken(token) : null;
  const roles = new Set(session?.roles ?? []);
  const canOpenSalesInvoice = roles.has("Admin") || roles.has("Sales") || roles.has("Finance");

  const [handover, jobs, customers, estimates, items] = await Promise.all([
    backendFetchJson<ServiceHandoverDto>(`/service/handovers/${id}`),
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=500"),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<ServiceEstimateSummaryDto[]>("/service/estimates?take=500"),
    backendFetchJson<ItemDto[]>("/items/options"),
  ]);

  const jobById = new Map(jobs.map((j) => [j.id, j]));
  const customerById = new Map(customers.map((c) => [c.id, c]));
  const job = jobById.get(handover.serviceJobId);
  const customer = job ? customerById.get(job.customerId) : null;
  const isDraft = handover.status === 0;
  const isCompleted = handover.status === 1;
  const handoverJobEstimates = estimates.filter((e) => e.serviceJobId === handover.serviceJobId);

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/service/handovers" className="hover:underline">
            Service Handovers
          </Link>{" "}
          / <span className="font-mono text-xs">{handover.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Handover {handover.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>
            Job:{" "}
            <TransactionLink referenceType="SJ" referenceId={handover.serviceJobId} monospace>
              {job?.number ?? handover.serviceJobId}
            </TransactionLink>
          </div>
          <div>Customer: {customer ? customer.code : "-"}</div>
          <div>Status: {statusLabel[handover.status] ?? handover.status}</div>
          <div>Date: {new Date(handover.handoverDate).toLocaleString()}</div>
          <div>
            Warranty:{" "}
            {typeof handover.postServiceWarrantyMonths === "number"
              ? `${handover.postServiceWarrantyMonths} month(s)`
              : "-"}
          </div>
          <div>
            Invoice:{" "}
            {handover.salesInvoiceId ? (
              canOpenSalesInvoice ? (
                <TransactionLink referenceType="INV" referenceId={handover.salesInvoiceId}>
                  {handover.salesInvoiceNumber ?? handover.salesInvoiceId}
                </TransactionLink>
              ) : (
                handover.salesInvoiceNumber ?? handover.salesInvoiceId
              )
            ) : (
              "-"
            )}
          </div>
        </div>
      </div>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink href={`/api/backend/service/handovers/${handover.id}/pdf`} target="_blank" rel="noopener noreferrer">
            Download PDF
          </SecondaryLink>
        </div>
        <ServiceHandoverActions handoverId={handover.id} canComplete={isDraft} canCancel={isDraft} />
        <div className="mt-3">
          <ServiceHandoverConvertInvoiceForm
            handoverId={handover.id}
            estimates={handoverJobEstimates}
            items={items}
            disabled={!isCompleted}
            existingSalesInvoiceId={handover.salesInvoiceId}
            redirectToSalesInvoice={canOpenSalesInvoice}
          />
          {handover.convertedToInvoiceAt ? (
            <div className="mt-2 text-xs text-zinc-500">
              Converted at {new Date(handover.convertedToInvoiceAt).toLocaleString()}
            </div>
          ) : null}
        </div>
      </Card>

      <Card>
        <div className="mb-2 text-sm font-semibold">Items Returned</div>
        <div className="whitespace-pre-wrap text-sm text-zinc-700 dark:text-zinc-200">{handover.itemsReturned}</div>
      </Card>

      <Card>
        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <div className="mb-2 text-sm font-semibold">Customer Acknowledgement</div>
            <div className="whitespace-pre-wrap text-sm text-zinc-700 dark:text-zinc-200">
              {handover.customerAcknowledgement || "-"}
            </div>
          </div>
          <div>
            <div className="mb-2 text-sm font-semibold">Notes</div>
            <div className="whitespace-pre-wrap text-sm text-zinc-700 dark:text-zinc-200">{handover.notes || "-"}</div>
          </div>
        </div>
      </Card>

      <DocumentCollaborationPanel
        referenceType="SH"
        referenceId={handover.id}
        title="Handover Comments & Attachments"
      />
    </div>
  );
}
