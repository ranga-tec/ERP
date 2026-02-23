import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, SecondaryLink, Table } from "@/components/ui";
import { PaymentAllocateForm } from "../PaymentAllocateForm";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";

type PaymentAllocationDto = { id: string; accountsReceivableEntryId?: string | null; accountsPayableEntryId?: string | null; amount: number };

type PaymentDetailDto = {
  id: string;
  referenceNumber: string;
  direction: number;
  counterpartyType: number;
  counterpartyId: string;
  amount: number;
  paidAt: string;
  notes?: string | null;
  allocations: PaymentAllocationDto[];
};

type CustomerDto = { id: string; code: string; name: string };
type SupplierDto = { id: string; code: string; name: string };

type ArDto = { id: string; customerId: string; referenceType: string; referenceId: string; amount: number; outstanding: number; postedAt: string };
type ApDto = { id: string; supplierId: string; referenceType: string; referenceId: string; amount: number; outstanding: number; postedAt: string };

const directionLabel: Record<number, string> = { 1: "Incoming", 2: "Outgoing" };
const counterpartyLabel: Record<number, string> = { 1: "Customer", 2: "Supplier" };

function referenceHref(type: string, id: string): string | null {
  if (type === "INV") return `/sales/invoices/${id}`;
  if (type === "DN") return `/sales/dispatches/${id}`;
  if (type === "GRN") return `/procurement/goods-receipts/${id}`;
  if (type === "SR") return `/procurement/supplier-returns/${id}`;
  if (type === "MR") return `/service/material-requisitions/${id}`;
  if (type === "ADJ") return `/inventory/stock-adjustments/${id}`;
  if (type === "TRF") return `/inventory/stock-transfers/${id}`;
  return null;
}

export default async function PaymentDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [payment, customers, suppliers] = await Promise.all([
    backendFetchJson<PaymentDetailDto>(`/finance/payments/${id}`),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<SupplierDto[]>("/suppliers"),
  ]);

  const customerById = new Map(customers.map((c) => [c.id, c]));
  const supplierById = new Map(suppliers.map((s) => [s.id, s]));

  const allocated = payment.allocations.reduce((sum, a) => sum + a.amount, 0);
  const remaining = Math.max(0, payment.amount - allocated);

  const isIncoming = payment.direction === 1;

  const [arEntries, apEntries] = await Promise.all([
    isIncoming ? backendFetchJson<ArDto[]>("/finance/ar?outstandingOnly=false") : Promise.resolve([] as ArDto[]),
    !isIncoming ? backendFetchJson<ApDto[]>("/finance/ap?outstandingOnly=false") : Promise.resolve([] as ApDto[]),
  ]);

  const arById = new Map(arEntries.map((e) => [e.id, e]));
  const apById = new Map(apEntries.map((e) => [e.id, e]));

  const allocateEntries = isIncoming
    ? arEntries.filter((e) => e.outstanding > 0 && (payment.counterpartyType === 1 ? e.customerId === payment.counterpartyId : true))
    : apEntries.filter((e) => e.outstanding > 0 && (payment.counterpartyType === 2 ? e.supplierId === payment.counterpartyId : true));

  const counterpartyCode =
    payment.counterpartyType === 1
      ? customerById.get(payment.counterpartyId)?.code ?? payment.counterpartyId
      : payment.counterpartyType === 2
        ? supplierById.get(payment.counterpartyId)?.code ?? payment.counterpartyId
        : payment.counterpartyId;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/finance/payments" className="hover:underline">
            Payments
          </Link>{" "}
          / <span className="font-mono text-xs">{payment.referenceNumber}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Payment {payment.referenceNumber}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>Direction: {directionLabel[payment.direction] ?? payment.direction}</div>
          <div>
            Counterparty: {counterpartyLabel[payment.counterpartyType] ?? payment.counterpartyType} {counterpartyCode}
          </div>
          <div>Paid: {new Date(payment.paidAt).toLocaleString()}</div>
          <div>Amount: {payment.amount}</div>
          <div>Allocated: {allocated}</div>
          <div>Remaining: {remaining}</div>
        </div>
        {payment.notes ? <div className="mt-2 text-sm text-zinc-500">Notes: {payment.notes}</div> : null}
      </div>

      <Card>
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink
            href={`/api/backend/finance/payments/${payment.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Allocations</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Target</th>
                <th className="py-2 pr-3">Entry</th>
                <th className="py-2 pr-3">Amount</th>
              </tr>
            </thead>
            <tbody>
              {payment.allocations.map((a) => {
                const target = a.accountsReceivableEntryId ? "AR" : a.accountsPayableEntryId ? "AP" : "—";
                const entryId = a.accountsReceivableEntryId ?? a.accountsPayableEntryId ?? "";
                const entry = a.accountsReceivableEntryId ? arById.get(a.accountsReceivableEntryId) : a.accountsPayableEntryId ? apById.get(a.accountsPayableEntryId) : undefined;
                const ref = entry ? `${entry.referenceType}:${entry.referenceId.slice(0, 8)}` : entryId ? entryId.slice(0, 8) : "—";
                const href = entry ? referenceHref(entry.referenceType, entry.referenceId) : null;
                return (
                  <tr key={a.id} className="border-b border-zinc-100 dark:border-zinc-900">
                    <td className="py-2 pr-3">{target}</td>
                    <td className="py-2 pr-3 font-mono text-xs">
                      {href ? (
                        <Link className="hover:underline" href={href}>
                          {ref}
                        </Link>
                      ) : (
                        ref
                      )}
                    </td>
                    <td className="py-2 pr-3">{a.amount}</td>
                  </tr>
                );
              })}
              {payment.allocations.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={3}>
                    No allocations yet.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>

      {remaining > 0 ? (
        <Card>
          <div className="mb-3 text-sm font-semibold">Allocate</div>
          {allocateEntries.length ? (
            <PaymentAllocateForm
              paymentId={payment.id}
              kind={isIncoming ? "ar" : "ap"}
              entries={allocateEntries.map((e) => ({ id: e.id, referenceType: e.referenceType, referenceId: e.referenceId, outstanding: e.outstanding }))}
              maxAmount={remaining}
            />
          ) : (
            <div className="text-sm text-zinc-500">No matching outstanding entries found for this counterparty.</div>
          )}
        </Card>
      ) : null}

      <DocumentCollaborationPanel referenceType="PAY" referenceId={id} />
    </div>
  );
}