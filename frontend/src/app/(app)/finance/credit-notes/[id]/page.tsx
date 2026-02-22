import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, SecondaryLink, Table } from "@/components/ui";
import { CreditNoteActions } from "../CreditNoteActions";

type CreditNoteDto = {
  id: string;
  referenceNumber: string;
  counterpartyType: number;
  counterpartyId: string;
  amount: number;
  remainingAmount: number;
  issuedAt: string;
  notes?: string | null;
  sourceReferenceType?: string | null;
  sourceReferenceId?: string | null;
};

type CreditNoteAllocationDto = {
  id: string;
  accountsReceivableEntryId?: string | null;
  accountsPayableEntryId?: string | null;
  amount: number;
};

type CreditNoteDetailDto = {
  creditNote: CreditNoteDto;
  allocations: CreditNoteAllocationDto[];
};

type CustomerDto = { id: string; code: string; name: string };
type SupplierDto = { id: string; code: string; name: string };

type ArDto = {
  id: string;
  customerId: string;
  referenceType: string;
  referenceId: string;
  amount: number;
  outstanding: number;
  postedAt: string;
};

type ApDto = {
  id: string;
  supplierId: string;
  referenceType: string;
  referenceId: string;
  amount: number;
  outstanding: number;
  postedAt: string;
};

const counterpartyLabel: Record<number, string> = { 1: "Customer", 2: "Supplier" };

export default async function CreditNoteDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [detail, customers, suppliers] = await Promise.all([
    backendFetchJson<CreditNoteDetailDto>(`/finance/credit-notes/${id}`),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<SupplierDto[]>("/suppliers"),
  ]);

  const note = detail.creditNote;
  const customerById = new Map(customers.map((c) => [c.id, c]));
  const supplierById = new Map(suppliers.map((s) => [s.id, s]));

  const allocateMode = note.counterpartyType === 1 ? "ar" : "ap";

  const entries = note.counterpartyType === 1
    ? await backendFetchJson<ArDto[]>("/finance/ar?outstandingOnly=false")
    : await backendFetchJson<ApDto[]>("/finance/ap?outstandingOnly=false");

  const filteredEntries =
    note.counterpartyType === 1
      ? (entries as ArDto[]).filter((e) => e.customerId === note.counterpartyId)
      : (entries as ApDto[]).filter((e) => e.supplierId === note.counterpartyId);

  const entryRefById = new Map(
    filteredEntries.map((e) => [
      e.id,
      `${e.referenceType}:${e.referenceId} (outstanding ${e.outstanding})`,
    ]),
  );

  const counterpartyCode =
    note.counterpartyType === 1
      ? customerById.get(note.counterpartyId)?.code ?? note.counterpartyId
      : supplierById.get(note.counterpartyId)?.code ?? note.counterpartyId;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/finance/credit-notes" className="hover:underline">
            Credit Notes
          </Link>{" "}
          / <span className="font-mono text-xs">{note.referenceNumber}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Credit Note {note.referenceNumber}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>
            Counterparty:{" "}
            <span className="font-medium text-zinc-900 dark:text-zinc-100">
              {counterpartyLabel[note.counterpartyType] ?? note.counterpartyType}: {counterpartyCode}
            </span>
          </div>
          <div>Issued: {new Date(note.issuedAt).toLocaleString()}</div>
          <div>Amount: {note.amount}</div>
          <div>Remaining: {note.remainingAmount}</div>
        </div>
        <div className="mt-2 text-sm text-zinc-500">
          Source: {note.sourceReferenceType ? `${note.sourceReferenceType}:${note.sourceReferenceId ?? ""}` : "—"} · Notes:{" "}
          {note.notes ?? "—"}
        </div>
      </div>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink
            href={`/api/backend/finance/credit-notes/${note.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
        <CreditNoteActions creditNoteId={note.id} allocateMode={allocateMode} entries={filteredEntries} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Allocations</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Entry</th>
                <th className="py-2 pr-3">Amount</th>
              </tr>
            </thead>
            <tbody>
              {detail.allocations.map((a) => {
                const entryId = a.accountsReceivableEntryId ?? a.accountsPayableEntryId ?? "";
                const entry = entryRefById.get(entryId) ?? entryId;
                return (
                  <tr key={a.id} className="border-b border-zinc-100 dark:border-zinc-900">
                    <td className="py-2 pr-3 font-mono text-xs">{entry || "—"}</td>
                    <td className="py-2 pr-3">{a.amount}</td>
                  </tr>
                );
              })}
              {detail.allocations.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={2}>
                    No allocations yet.
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

