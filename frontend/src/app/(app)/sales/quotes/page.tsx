import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { QuoteCreateForm } from "./QuoteCreateForm";

type CustomerDto = { id: string; code: string; name: string };
type SalesQuoteSummaryDto = {
  id: string;
  number: string;
  customerId: string;
  quoteDate: string;
  validUntil?: string | null;
  status: number;
  total: number;
};

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Sent",
  2: "Accepted",
  3: "Rejected",
  4: "Expired",
  5: "Cancelled",
};

export default async function QuotesPage() {
  const [quotes, customers] = await Promise.all([
    backendFetchJson<SalesQuoteSummaryDto[]>("/sales/quotes?take=100"),
    backendFetchJson<CustomerDto[]>("/customers"),
  ]);

  const customerById = new Map(customers.map((c) => [c.id, c]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Sales Quotes</h1>
        <p className="mt-1 text-sm text-zinc-500">Draft → add lines → mark sent.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <QuoteCreateForm customers={customers} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Customer</th>
                <th className="py-2 pr-3">Quote Date</th>
                <th className="py-2 pr-3">Valid Until</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Total</th>
              </tr>
            </thead>
            <tbody>
              {quotes.map((q) => (
                <tr key={q.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/sales/quotes/${q.id}`}>
                      {q.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">{customerById.get(q.customerId)?.code ?? q.customerId}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(q.quoteDate).toLocaleString()}</td>
                  <td className="py-2 pr-3 text-zinc-500">
                    {q.validUntil ? new Date(q.validUntil).toLocaleDateString() : "—"}
                  </td>
                  <td className="py-2 pr-3">{statusLabel[q.status] ?? q.status}</td>
                  <td className="py-2 pr-3">{q.total}</td>
                </tr>
              ))}
              {quotes.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={6}>
                    No quotes yet.
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

