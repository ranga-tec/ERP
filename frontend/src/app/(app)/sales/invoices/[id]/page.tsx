import Link from "next/link";
import { cookies } from "next/headers";
import { backendFetchJson } from "@/lib/backend.server";
import { ISS_TOKEN_COOKIE } from "@/lib/env";
import { sessionFromToken } from "@/lib/jwt";
import { ItemInlineLink } from "@/components/InlineLink";
import { Card, SecondaryLink } from "@/components/ui";
import { InvoiceActions } from "../InvoiceActions";
import { InvoiceLineAddForm } from "../InvoiceLineAddForm";
import { InvoiceLinesEditor } from "../InvoiceLinesEditor";
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
    revenueAccountId?: string | null;
    revenueAccountCode?: string | null;
    revenueAccountName?: string | null;
    quantity: number;
    unitPrice: number;
    discountPercent: number;
    taxPercent: number;
    lineTotal: number;
  }[];
};

type CustomerDto = { id: string; code: string; name: string };
type ItemDto = { id: string; sku: string; name: string };
type TaxDto = { id: string; code: string; name: string; ratePercent: number; isActive: boolean };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Paid",
  3: "Voided",
};

export default async function InvoiceDetailPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ mode?: string }>;
}) {
  const { id } = await params;
  const { mode } = await searchParams;
  const startInEditMode = mode === "edit";
  const cookieStore = await cookies();
  const token = cookieStore.get(ISS_TOKEN_COOKIE)?.value;
  const session = token ? sessionFromToken(token) : null;
  const roles = new Set(session?.roles ?? []);
  const canManageInvoices = roles.has("Admin") || roles.has("Sales") || roles.has("Finance");

  const [invoice, customers, items, taxes] = await Promise.all([
    backendFetchJson<InvoiceDto>(`/sales/invoices/${id}`),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<ItemDto[]>("/items"),
    canManageInvoices ? backendFetchJson<TaxDto[]>("/taxes") : Promise.resolve([] as TaxDto[]),
  ]);

  const customerById = new Map(customers.map((c) => [c.id, c]));
  const itemLabelById = new Map(
    items.map((item) => [
      item.id,
      <ItemInlineLink key={item.id} itemId={item.id}>
        {`${item.sku} - ${item.name}`}
      </ItemInlineLink>,
    ]),
  );
  const itemSearchLabelById = new Map(
    items.map((item) => [item.id, `${item.sku} ${item.name}`.toLowerCase()]),
  );
  const isDraft = invoice.status === 0;
  const unresolvedRevenueLineCount = invoice.lines.filter((line) => !line.revenueAccountId).length;

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
        {canManageInvoices ? (
          <InvoiceActions invoiceId={invoice.id} canPost={isDraft && invoice.lines.length > 0} />
        ) : (
          <div className="text-sm text-zinc-500">Read-only access.</div>
        )}
      </Card>

      {isDraft && canManageInvoices ? (
        <Card>
          <div className="mb-3 text-sm font-semibold">Add line</div>
          <InvoiceLineAddForm invoiceId={invoice.id} items={items} taxes={taxes} />
        </Card>
      ) : null}

      <Card>
        <div className="mb-3 text-sm font-semibold">Lines</div>
        <InvoiceLinesEditor
          invoiceId={invoice.id}
          lines={invoice.lines}
          itemLabelById={itemLabelById}
          itemSearchLabelById={itemSearchLabelById}
          startInEditMode={startInEditMode}
          canEdit={isDraft && canManageInvoices}
        />
      </Card>

      {unresolvedRevenueLineCount > 0 ? (
        <Card>
          <div className="text-sm text-amber-700 dark:text-amber-300">
            {unresolvedRevenueLineCount} invoice line(s) do not have a resolved income account from the item or item category mapping.
          </div>
        </Card>
      ) : null}

      <DocumentCollaborationPanel referenceType="INV" referenceId={id} />
    </div>
  );
}

