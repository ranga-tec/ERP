import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { InvoiceCreateForm } from "./InvoiceCreateForm";

type CustomerDto = { id: string; code: string; name: string };
type InvoiceSummaryDto = {
  id: string;
  number: string;
  customerId: string;
  invoiceDate: string;
  dueDate?: string | null;
  status: number;
  total: number;
};

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Paid",
  3: "Voided",
};

export default async function InvoicesPage() {
  const [invoices, customers] = await Promise.all([
    backendFetchJson<InvoiceSummaryDto[]>("/sales/invoices?take=100"),
    backendFetchJson<CustomerDto[]>("/customers"),
  ]);

  const customerById = new Map(customers.map((c) => [c.id, c]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Sales Invoices</h1>
        <p className="mt-1 text-sm text-zinc-500">Draft → add lines → post → pay (via payments).</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <InvoiceCreateForm customers={customers} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Customer</th>
                <th className="py-2 pr-3">Invoice Date</th>
                <th className="py-2 pr-3">Due Date</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Total</th>
              </tr>
            </thead>
            <tbody>
              {invoices.map((i) => (
                <tr key={i.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/sales/invoices/${i.id}`}>
                      {i.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">{customerById.get(i.customerId)?.code ?? i.customerId}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(i.invoiceDate).toLocaleString()}</td>
                  <td className="py-2 pr-3 text-zinc-500">
                    {i.dueDate ? new Date(i.dueDate).toLocaleDateString() : "—"}
                  </td>
                  <td className="py-2 pr-3">{statusLabel[i.status] ?? i.status}</td>
                  <td className="py-2 pr-3">{i.total}</td>
                </tr>
              ))}
              {invoices.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={6}>
                    No invoices yet.
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

