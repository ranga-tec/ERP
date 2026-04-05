import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { ItemInlineLink } from "@/components/InlineLink";
import { Card, SecondaryLink } from "@/components/ui";
import { QuoteActions } from "../QuoteActions";
import { QuoteLineAddForm } from "../QuoteLineAddForm";
import { QuoteLinesEditor } from "../QuoteLinesEditor";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";

type SalesQuoteDto = {
  id: string;
  number: string;
  customerId: string;
  quoteDate: string;
  validUntil?: string | null;
  status: number;
  total: number;
  lines: { id: string; itemId: string; quantity: number; unitPrice: number; lineTotal: number }[];
};

type CustomerDto = { id: string; code: string; name: string };
type ItemDto = { id: string; sku: string; name: string };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Sent",
  2: "Accepted",
  3: "Rejected",
  4: "Expired",
  5: "Cancelled",
};

export default async function QuoteDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [quote, customers, items] = await Promise.all([
    backendFetchJson<SalesQuoteDto>(`/sales/quotes/${id}`),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<ItemDto[]>("/items"),
  ]);

  const customerById = new Map(customers.map((c) => [c.id, c]));
  const isDraft = quote.status === 0;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/sales/quotes" className="hover:underline">
            Quotes
          </Link>{" "}
          / <span className="font-mono text-xs">{quote.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Quote {quote.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>
            Customer:{" "}
            <span className="font-medium text-zinc-900 dark:text-zinc-100">
              {customerById.get(quote.customerId)?.code ?? quote.customerId}
            </span>
          </div>
          <div>Status: {statusLabel[quote.status] ?? quote.status}</div>
          <div>Date: {new Date(quote.quoteDate).toLocaleString()}</div>
          <div>Valid until: {quote.validUntil ? new Date(quote.validUntil).toLocaleDateString() : "—"}</div>
          <div>Total: {quote.total}</div>
        </div>
      </div>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink
            href={`/api/backend/sales/quotes/${quote.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
        <QuoteActions quoteId={quote.id} canSend={isDraft && quote.lines.length > 0} />
      </Card>

      {isDraft ? (
        <Card>
          <div className="mb-3 text-sm font-semibold">Add line</div>
          <QuoteLineAddForm quoteId={quote.id} items={items} />
        </Card>
      ) : null}

      <Card>
        <div className="mb-3 text-sm font-semibold">Lines</div>
        <QuoteLinesEditor
          quoteId={quote.id}
          lines={quote.lines}
          itemLabelById={new Map(
            items.map((item) => [
              item.id,
              <ItemInlineLink key={item.id} itemId={item.id}>
                {`${item.sku} - ${item.name}`}
              </ItemInlineLink>,
            ]),
          )}
          itemSearchLabelById={new Map(items.map((item) => [item.id, `${item.sku} ${item.name}`.toLowerCase()]))}
          canEdit={isDraft}
        />
      </Card>

      <DocumentCollaborationPanel referenceType="SQ" referenceId={id} />
    </div>
  );
}

