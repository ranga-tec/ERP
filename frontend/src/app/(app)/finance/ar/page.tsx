import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { ListViewEditActions } from "@/components/ListViewEditActions";
import { TableSearchInput } from "@/components/TableSearchInput";
import { TransactionLink } from "@/components/TransactionLink";
import { Button, Card, Select, Table } from "@/components/ui";
import { buildReferenceRouteMap, resolveReferenceHref } from "@/lib/reference-routing";

type ArDto = {
  id: string;
  customerId: string;
  referenceType: string;
  referenceId: string;
  referenceNumber: string;
  amount: number;
  outstanding: number;
  postedAt: string;
};

type CustomerDto = { id: string; code: string; name: string };
type ReferenceFormDto = { code: string; routeTemplate?: string | null; isActive: boolean };
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
  sourceReferenceNumber?: string | null;
};

export default async function AccountsReceivablePage({ searchParams }: { searchParams?: Promise<{ outstandingOnly?: string }> }) {
  const sp = await searchParams;
  const outstandingOnly = (sp?.outstandingOnly ?? "true") !== "false";

  const [entries, customers, referenceForms, creditNotes] = await Promise.all([
    backendFetchJson<ArDto[]>(`/finance/ar?outstandingOnly=${outstandingOnly ? "true" : "false"}`),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<ReferenceFormDto[]>("/reference-forms"),
    backendFetchJson<CreditNoteDto[]>("/finance/credit-notes?counterpartyType=1"),
  ]);

  const customerById = new Map(customers.map((c) => [c.id, c]));
  const referenceRouteMap = buildReferenceRouteMap(referenceForms);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Accounts Receivable</h1>
        <p className="mt-1 text-sm text-zinc-500">Customer charges and credits. Credit notes reduce AR balances through allocations.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Filter</div>
        <form method="GET" className="flex flex-wrap items-end gap-3">
          <div className="min-w-56">
            <label className="mb-1 block text-sm font-medium">Show</label>
            <Select name="outstandingOnly" defaultValue={outstandingOnly ? "true" : "false"}>
              <option value="true">Outstanding only</option>
              <option value="false">All entries</option>
            </Select>
          </div>
          <Button type="submit">Apply</Button>
        </form>
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Entries</div>
        <TableSearchInput placeholder="Search accounts receivable..." />
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Posted</th>
                <th className="py-2 pr-3">Customer</th>
                <th className="py-2 pr-3">Ref</th>
                <th className="py-2 pr-3">Amount</th>
                <th className="py-2 pr-3">Outstanding</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {entries.map((e) => {
                const href = resolveReferenceHref(referenceRouteMap, e.referenceType, e.referenceId);
                return (
                  <tr key={e.id} className="border-b border-zinc-100 dark:border-zinc-900">
                    <td className="py-2 pr-3 text-zinc-500">{new Date(e.postedAt).toLocaleString()}</td>
                    <td className="py-2 pr-3">{customerById.get(e.customerId)?.code ?? e.customerId}</td>
                    <td className="py-2 pr-3 font-mono text-xs">
                      {href ? (
                        <Link className="hover:underline" href={href}>
                          {e.referenceType}:{e.referenceNumber}
                        </Link>
                      ) : (
                        <span>
                          {e.referenceType}:{e.referenceNumber}
                        </span>
                      )}
                    </td>
                    <td className="py-2 pr-3">{e.amount}</td>
                    <td className="py-2 pr-3">{e.outstanding}</td>
                    <td className="py-2 pr-3">
                      {href ? (
                        <ListViewEditActions viewHref={href} canEdit={false} />
                      ) : (
                        <span className="text-xs text-zinc-400">No link</span>
                      )}
                    </td>
                  </tr>
                );
              })}
              {entries.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={6}>
                    No AR entries.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Customer Credit Notes</div>
        <TableSearchInput placeholder="Search customer credit notes..." />
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Reference</th>
                <th className="py-2 pr-3">Customer</th>
                <th className="py-2 pr-3">Issued</th>
                <th className="py-2 pr-3">Amount</th>
                <th className="py-2 pr-3">Unallocated</th>
                <th className="py-2 pr-3">Source</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {creditNotes.map((note) => (
                <tr key={note.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/finance/credit-notes/${note.id}`}>
                      {note.referenceNumber}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">{customerById.get(note.counterpartyId)?.code ?? note.counterpartyId}</td>
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
                  <td className="py-2 pr-3">
                    <ListViewEditActions viewHref={`/finance/credit-notes/${note.id}`} canEdit={false} />
                  </td>
                </tr>
              ))}
              {creditNotes.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={7}>
                    No customer credit notes.
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
