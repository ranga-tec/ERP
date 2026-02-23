import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, SecondaryLink, Table } from "@/components/ui";
import { InvoiceActions } from "../InvoiceActions";
import { InvoiceLineAddForm } from "../InvoiceLineAddForm";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";

type InvoiceDto = {
  id: string;
  number: string;
  customerId: string;
  invoiceDate: string;
  dueDate?: string | null;
  status: number;
  subtotal: number;
  taxTotal: number;
  total: number;
  lines: {
    id: string;
    itemId: string;
    quantity: number;
    unitPrice: number;
    discountPercent: number;
    taxPercent: number;
    lineTotal: number;
  }[];
};

type CustomerDto = { id: string; code: string; name: string };
type ItemDto = { id: string; sku: string; name: string };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Paid",
  3: "Voided",
};

export default async function InvoiceDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [invoice, customers, items] = await Promise.all([
    backendFetchJson<InvoiceDto>(`/sales/invoices/${id}`),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<ItemDto[]>("/items"),
  ]);

  const customerById = new Map(customers.map((c) => [c.id, c]));
  const itemById = new Map(items.map((i) => [i.id, i]));
  const isDraft = invoice.status === 0;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/sales/invoices" className="hover:underline">
            Invoices
          </Link>{" "}
          / <span className="font-mono text-xs">{invoice.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Invoice {invoice.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>
            Customer:{" "}
            <span className="font-medium text-zinc-900 dark:text-zinc-100">
              {customerById.get(invoice.customerId)?.code ?? invoice.customerId}
            </span>
          </div>
          <div>Status: {statusLabel[invoice.status] ?? invoice.status}</div>
          <div>Date: {new Date(invoice.invoiceDate).toLocaleString()}</div>
          <div>Due: {invoice.dueDate ? new Date(invoice.dueDate).toLocaleDateString() : "—"}</div>
          <div>Total: {invoice.total}</div>
        </div>
        <div className="mt-2 text-sm text-zinc-500">
          Subtotal: {invoice.subtotal} · Tax: {invoice.taxTotal}
        </div>
      </div>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink
            href={`/api/backend/sales/invoices/${invoice.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
        <InvoiceActions invoiceId={invoice.id} canPost={isDraft && invoice.lines.length > 0} />
      </Card>

      {isDraft ? (
        <Card>
          <div className="mb-3 text-sm font-semibold">Add line</div>
          <InvoiceLineAddForm invoiceId={invoice.id} items={items} />
        </Card>
      ) : null}

      <Card>
        <div className="mb-3 text-sm font-semibold">Lines</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Item</th>
                <th className="py-2 pr-3">Qty</th>
                <th className="py-2 pr-3">Unit Price</th>
                <th className="py-2 pr-3">Disc %</th>
                <th className="py-2 pr-3">Tax %</th>
                <th className="py-2 pr-3">Line Total</th>
              </tr>
            </thead>
            <tbody>
              {invoice.lines.map((l) => (
                <tr key={l.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3">{itemById.get(l.itemId)?.sku ?? l.itemId}</td>
                  <td className="py-2 pr-3">{l.quantity}</td>
                  <td className="py-2 pr-3">{l.unitPrice}</td>
                  <td className="py-2 pr-3">{l.discountPercent}</td>
                  <td className="py-2 pr-3">{l.taxPercent}</td>
                  <td className="py-2 pr-3">{l.lineTotal}</td>
                </tr>
              ))}
              {invoice.lines.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={6}>
                    No lines yet.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>

      <DocumentCollaborationPanel referenceType="INV" referenceId={id} />
    </div>
  );
}