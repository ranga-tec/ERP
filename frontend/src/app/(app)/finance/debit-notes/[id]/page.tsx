import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, SecondaryLink } from "@/components/ui";

type DebitNoteDto = {
  id: string;
  referenceNumber: string;
  counterpartyType: number;
  counterpartyId: string;
  amount: number;
  issuedAt: string;
  notes?: string | null;
  sourceReferenceType?: string | null;
  sourceReferenceId?: string | null;
};

type CustomerDto = { id: string; code: string; name: string };
type SupplierDto = { id: string; code: string; name: string };

const counterpartyLabel: Record<number, string> = { 1: "Customer", 2: "Supplier" };

export default async function DebitNoteDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [note, customers, suppliers] = await Promise.all([
    backendFetchJson<DebitNoteDto>(`/finance/debit-notes/${id}`),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<SupplierDto[]>("/suppliers"),
  ]);

  const customerById = new Map(customers.map((c) => [c.id, c]));
  const supplierById = new Map(suppliers.map((s) => [s.id, s]));

  const counterpartyCode =
    note.counterpartyType === 1
      ? customerById.get(note.counterpartyId)?.code ?? note.counterpartyId
      : supplierById.get(note.counterpartyId)?.code ?? note.counterpartyId;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/finance/debit-notes" className="hover:underline">
            Debit Notes
          </Link>{" "}
          / <span className="font-mono text-xs">{note.referenceNumber}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Debit Note {note.referenceNumber}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>
            Counterparty:{" "}
            <span className="font-medium text-zinc-900 dark:text-zinc-100">
              {counterpartyLabel[note.counterpartyType] ?? note.counterpartyType}: {counterpartyCode}
            </span>
          </div>
          <div>Issued: {new Date(note.issuedAt).toLocaleString()}</div>
          <div>Amount: {note.amount}</div>
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
            href={`/api/backend/finance/debit-notes/${note.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
        <div className="text-sm text-zinc-500">Debit notes create AR/AP charges automatically.</div>
      </Card>
    </div>
  );
}

