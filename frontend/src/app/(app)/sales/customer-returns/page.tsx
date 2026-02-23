import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { CustomerReturnCreateForm } from "./CustomerReturnCreateForm";

type CustomerReturnSummaryDto = {
  id: string;
  number: string;
  customerId: string;
  warehouseId: string;
  returnDate: string;
  status: number;
  salesInvoiceId?: string | null;
  dispatchNoteId?: string | null;
  reason?: string | null;
};

type CustomerDto = { id: string; code: string; name: string };
type WarehouseDto = { id: string; code: string; name: string };
type InvoiceSummaryDto = { id: string; number: string; customerId: string; total: number; status: number };
type DispatchSummaryDto = { id: string; number: string; salesOrderId: string; status: number };
type SalesOrderSummaryDto = { id: string; number: string; customerId: string };

const statusLabel: Record<number, string> = { 0: "Draft", 1: "Posted", 2: "Voided" };

export default async function CustomerReturnsPage() {
  const [rows, customers, warehouses, invoices, dispatches, orders] = await Promise.all([
    backendFetchJson<CustomerReturnSummaryDto[]>("/sales/customer-returns?take=100"),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<InvoiceSummaryDto[]>("/sales/invoices?take=200"),
    backendFetchJson<DispatchSummaryDto[]>("/sales/dispatches?take=200"),
    backendFetchJson<SalesOrderSummaryDto[]>("/sales/orders?take=500"),
  ]);

  const customerById = new Map(customers.map((c) => [c.id, c]));
  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));
  const invoiceById = new Map(invoices.map((i) => [i.id, i]));
  const dispatchById = new Map(dispatches.map((d) => [d.id, d]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Customer Returns</h1>
        <p className="mt-1 text-sm text-zinc-500">Receive returned goods, post stock-in, and auto-create customer credit notes.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <CustomerReturnCreateForm
          customers={customers}
          warehouses={warehouses}
          invoices={invoices}
          dispatches={dispatches}
          salesOrders={orders}
        />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Customer</th>
                <th className="py-2 pr-3">Warehouse</th>
                <th className="py-2 pr-3">Date</th>
                <th className="py-2 pr-3">Invoice</th>
                <th className="py-2 pr-3">Dispatch</th>
                <th className="py-2 pr-3">Status</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => (
                <tr key={r.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/sales/customer-returns/${r.id}`}>
                      {r.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">{customerById.get(r.customerId)?.code ?? r.customerId}</td>
                  <td className="py-2 pr-3">{warehouseById.get(r.warehouseId)?.code ?? r.warehouseId}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(r.returnDate).toLocaleString()}</td>
                  <td className="py-2 pr-3 font-mono text-xs">{r.salesInvoiceId ? invoiceById.get(r.salesInvoiceId)?.number ?? r.salesInvoiceId : "-"}</td>
                  <td className="py-2 pr-3 font-mono text-xs">{r.dispatchNoteId ? dispatchById.get(r.dispatchNoteId)?.number ?? r.dispatchNoteId : "-"}</td>
                  <td className="py-2 pr-3">{statusLabel[r.status] ?? r.status}</td>
                </tr>
              ))}
              {rows.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={7}>
                    No customer returns yet.
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
