import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { ListViewEditActions } from "@/components/ListViewEditActions";
import { TransactionLink } from "@/components/TransactionLink";
import { Card, Table } from "@/components/ui";
import { DebitNoteCreateForm } from "./DebitNoteCreateForm";

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

export default async function DebitNotesPage() {
  const [notes, customers, suppliers] = await Promise.all([
    backendFetchJson<DebitNoteDto[]>("/finance/debit-notes"),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<SupplierDto[]>("/suppliers"),
  ]);

  const customerById = new Map(customers.map((customer) => [customer.id, customer]));
  const supplierById = new Map(suppliers.map((supplier) => [supplier.id, supplier]));

  function counterpartyCode(type: number, id: string): string {
    if (type === 1) return customerById.get(id)?.code ?? id;
    if (type === 2) return supplierById.get(id)?.code ?? id;
    return id;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Debit Notes</h1>
        <p className="mt-1 text-sm text-zinc-500">Issue additional charges to customers/suppliers (AR/AP).</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <DebitNoteCreateForm customers={customers} suppliers={suppliers} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Reference</th>
                <th className="py-2 pr-3">Counterparty</th>
                <th className="py-2 pr-3">Issued</th>
                <th className="py-2 pr-3">Amount</th>
                <th className="py-2 pr-3">Source</th>
                <th className="py-2 pr-3">Notes</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {notes.map((note) => (
                <tr key={note.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/finance/debit-notes/${note.id}`}>
                      {note.referenceNumber}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">
                    {counterpartyLabel[note.counterpartyType] ?? note.counterpartyType}:{" "}
                    {counterpartyCode(note.counterpartyType, note.counterpartyId)}
                  </td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(note.issuedAt).toLocaleString()}</td>
                  <td className="py-2 pr-3">{note.amount}</td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-500">
                    {note.sourceReferenceType ? (
                      <TransactionLink referenceType={note.sourceReferenceType} referenceId={note.sourceReferenceId} monospace>
                        {`${note.sourceReferenceType}:${note.sourceReferenceId ?? ""}`}
                      </TransactionLink>
                    ) : (
                      "-"
                    )}
                  </td>
                  <td className="py-2 pr-3 text-zinc-500">{note.notes ?? "-"}</td>
                  <td className="py-2 pr-3">
                    <ListViewEditActions viewHref={`/finance/debit-notes/${note.id}`} canEdit={false} />
                  </td>
                </tr>
              ))}
              {notes.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={7}>
                    No debit notes yet.
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
