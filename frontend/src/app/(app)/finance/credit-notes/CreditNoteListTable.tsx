import Link from "next/link";
import { ListViewEditActions } from "@/components/ListViewEditActions";
import { TransactionLink } from "@/components/TransactionLink";
import { Table } from "@/components/ui";

export type CreditNoteListDto = {
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
  sourceReferenceNumber?: string | null;
};

type CounterpartyRef = { id: string; code: string; name: string };

export function CreditNoteListTable({
  notes,
  counterparties,
  emptyText,
}: {
  notes: CreditNoteListDto[];
  counterparties: CounterpartyRef[];
  emptyText: string;
}) {
  const counterpartyById = new Map(counterparties.map((counterparty) => [counterparty.id, counterparty]));

  return (
    <div className="overflow-auto">
      <Table>
        <thead>
          <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
            <th className="py-2 pr-3">Reference</th>
            <th className="py-2 pr-3">Counterparty</th>
            <th className="py-2 pr-3">Issued</th>
            <th className="py-2 pr-3">Amount</th>
            <th className="py-2 pr-3">Unallocated</th>
            <th className="py-2 pr-3">Source</th>
            <th className="py-2 pr-3">Notes</th>
            <th className="py-2 pr-3">Actions</th>
          </tr>
        </thead>
        <tbody>
          {notes.map((note) => (
            <tr key={note.id} className="border-b border-zinc-100 dark:border-zinc-900">
              <td className="py-2 pr-3 font-mono text-xs">
                <Link className="hover:underline" href={`/finance/credit-notes/${note.id}`}>
                  {note.referenceNumber}
                </Link>
              </td>
              <td className="py-2 pr-3">{counterpartyById.get(note.counterpartyId)?.code ?? note.counterpartyId}</td>
              <td className="py-2 pr-3 text-zinc-500">{new Date(note.issuedAt).toLocaleString()}</td>
              <td className="py-2 pr-3">{note.amount}</td>
              <td className="py-2 pr-3">{note.remainingAmount}</td>
              <td className="py-2 pr-3 font-mono text-xs text-zinc-500">
                {note.sourceReferenceType ? (
                  <TransactionLink referenceType={note.sourceReferenceType} referenceId={note.sourceReferenceId} monospace>
                    {note.sourceReferenceNumber
                      ? `${note.sourceReferenceType}:${note.sourceReferenceNumber}`
                      : `${note.sourceReferenceType}:source unavailable`}
                  </TransactionLink>
                ) : (
                  "-"
                )}
              </td>
              <td className="py-2 pr-3 text-zinc-500">{note.notes ?? "-"}</td>
              <td className="py-2 pr-3">
                <ListViewEditActions viewHref={`/finance/credit-notes/${note.id}`} canEdit={false} />
              </td>
            </tr>
          ))}
          {notes.length === 0 ? (
            <tr>
              <td className="py-6 text-sm text-zinc-500" colSpan={8}>
                {emptyText}
              </td>
            </tr>
          ) : null}
        </tbody>
      </Table>
    </div>
  );
}
