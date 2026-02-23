import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, SecondaryLink, Table } from "@/components/ui";
import { CustomerReturnActions } from "../CustomerReturnActions";
import { CustomerReturnLineAddForm } from "../CustomerReturnLineAddForm";

type CustomerReturnDto = {
  id: string;
  number: string;
  customerId: string;
  warehouseId: string;
  returnDate: string;
  status: number;
  salesInvoiceId?: string | null;
  dispatchNoteId?: string | null;
  reason?: string | null;
  lines: { id: string; itemId: string; quantity: number; unitPrice: number; batchNumber?: string | null; serials: string[] }[];
};

type CustomerDto = { id: string; code: string; name: string };
type WarehouseDto = { id: string; code: string; name: string };
type ItemDto = { id: string; sku: string; name: string; trackingType: number; defaultUnitCost: number };
type InvoiceSummaryDto = { id: string; number: string; customerId: string; total: number; status: number };
type DispatchSummaryDto = { id: string; number: string; salesOrderId: string; status: number };

const statusLabel: Record<number, string> = { 0: "Draft", 1: "Posted", 2: "Voided" };

export default async function CustomerReturnDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [cr, customers, warehouses, items, invoices, dispatches] = await Promise.all([
    backendFetchJson<CustomerReturnDto>(`/sales/customer-returns/${id}`),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<ItemDto[]>("/items"),
    backendFetchJson<InvoiceSummaryDto[]>("/sales/invoices?take=200"),
    backendFetchJson<DispatchSummaryDto[]>("/sales/dispatches?take=200"),
  ]);

  const customerById = new Map(customers.map((c) => [c.id, c]));
  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));
  const itemById = new Map(items.map((i) => [i.id, i]));
  const invoiceById = new Map(invoices.map((i) => [i.id, i]));
  const dispatchById = new Map(dispatches.map((d) => [d.id, d]));
  const isDraft = cr.status === 0;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/sales/customer-returns" className="hover:underline">
            Customer Returns
          </Link>{" "}
          / <span className="font-mono text-xs">{cr.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Customer Return {cr.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>Customer: {customerById.get(cr.customerId)?.code ?? cr.customerId}</div>
          <div>Warehouse: {warehouseById.get(cr.warehouseId)?.code ?? cr.warehouseId}</div>
          <div>Status: {statusLabel[cr.status] ?? cr.status}</div>
          <div>Date: {new Date(cr.returnDate).toLocaleString()}</div>
        </div>
        <div className="mt-2 text-sm text-zinc-500">
          Invoice: {cr.salesInvoiceId ? invoiceById.get(cr.salesInvoiceId)?.number ?? cr.salesInvoiceId : "-"} · Dispatch:{" "}
          {cr.dispatchNoteId ? dispatchById.get(cr.dispatchNoteId)?.number ?? cr.dispatchNoteId : "-"}
        </div>
        {cr.reason ? <div className="mt-2 text-sm text-zinc-500">Reason: {cr.reason}</div> : null}
      </div>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink
            href={`/api/backend/sales/customer-returns/${cr.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
        <CustomerReturnActions customerReturnId={cr.id} canPost={isDraft && cr.lines.length > 0} />
      </Card>

      {isDraft ? (
        <Card>
          <div className="mb-3 text-sm font-semibold">Add line</div>
          <CustomerReturnLineAddForm customerReturnId={cr.id} items={items} />
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
                <th className="py-2 pr-3">Batch</th>
                <th className="py-2 pr-3">Serials</th>
              </tr>
            </thead>
            <tbody>
              {cr.lines.map((l) => (
                <tr key={l.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3">{itemById.get(l.itemId)?.sku ?? l.itemId}</td>
                  <td className="py-2 pr-3">{l.quantity}</td>
                  <td className="py-2 pr-3">{l.unitPrice}</td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-500">{l.batchNumber ?? "-"}</td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-500">{l.serials.length ? l.serials.join(", ") : "-"}</td>
                </tr>
              ))}
              {cr.lines.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={5}>
                    No lines yet.
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
