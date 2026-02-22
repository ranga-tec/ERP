import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { CreditNoteCreateForm } from "./CreditNoteCreateForm";

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

type CustomerDto = { id: string; code: string; name: string };
type SupplierDto = { id: string; code: string; name: string };

const counterpartyLabel: Record<number, string> = { 1: "Customer", 2: "Supplier" };

export default async function CreditNotesPage() {
  const [notes, customers, suppliers] = await Promise.all([
    backendFetchJson<CreditNoteDto[]>("/finance/credit-notes"),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<SupplierDto[]>("/suppliers"),
  ]);

  const customerById = new Map(customers.map((c) => [c.id, c]));
  const supplierById = new Map(suppliers.map((s) => [s.id, s]));

  function counterpartyCode(type: number, id: string): string {
    if (type === 1) return customerById.get(id)?.code ?? id;
    if (type === 2) return supplierById.get(id)?.code ?? id;
    return id;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Credit Notes</h1>
        <p className="mt-1 text-sm text-zinc-500">Issue credits to customers/suppliers and allocate to AR/AP.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <CreditNoteCreateForm customers={customers} suppliers={suppliers} />
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
                <th className="py-2 pr-3">Remaining</th>
                <th className="py-2 pr-3">Source</th>
                <th className="py-2 pr-3">Notes</th>
              </tr>
            </thead>
            <tbody>
              {notes.map((n) => (
                <tr key={n.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/finance/credit-notes/${n.id}`}>
                      {n.referenceNumber}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">
                    {counterpartyLabel[n.counterpartyType] ?? n.counterpartyType}:{" "}
                    {counterpartyCode(n.counterpartyType, n.counterpartyId)}
                  </td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(n.issuedAt).toLocaleString()}</td>
                  <td className="py-2 pr-3">{n.amount}</td>
                  <td className="py-2 pr-3">{n.remainingAmount}</td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-500">
                    {n.sourceReferenceType ? `${n.sourceReferenceType}:${n.sourceReferenceId ?? ""}` : "—"}
                  </td>
                  <td className="py-2 pr-3 text-zinc-500">{n.notes ?? "—"}</td>
                </tr>
              ))}
              {notes.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={7}>
                    No credit notes yet.
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

