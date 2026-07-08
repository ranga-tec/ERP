import Link from "next/link";
import { cookies } from "next/headers";
import { backendFetchJson } from "@/lib/backend.server";
import { NEUEDGE_TOKEN_COOKIE } from "@/lib/env";
import { sessionFromToken } from "@/lib/jwt";
import { AppFormModal } from "@/components/AppFormModal";
import { ListViewEditActions } from "@/components/ListViewEditActions";
import { SearchableRow, SearchableTable } from "@/components/SearchableTable";
import { Card } from "@/components/ui";
import { InvoiceCreateForm } from "./InvoiceCreateForm";

type CustomerDto = { id: string; code: string; name: string };
type SalesOrderSummaryDto = {
  id: string;
  number: string;
  customerId: string;
};
type DispatchSummaryDto = {
  id: string;
  number: string;
  salesOrderId: string;
  dispatchedAt: string;
  status: number;
  lineCount: number;
};
type DirectDispatchSummaryDto = {
  id: string;
  number: string;
  customerId?: string | null;
  serviceJobId?: string | null;
  dispatchedAt: string;
  status: number;
  lineCount: number;
};
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
  const cookieStore = await cookies();
  const token = cookieStore.get(NEUEDGE_TOKEN_COOKIE)?.value;
  const session = token ? sessionFromToken(token) : null;
  const roles = new Set(session?.roles ?? []);
  const canManageInvoices = roles.has("Admin") || roles.has("Sales") || roles.has("Finance");

  const [invoices, customers, orders, dispatches, directDispatches] = await Promise.all([
    backendFetchJson<InvoiceSummaryDto[]>("/sales/invoices?take=100"),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<SalesOrderSummaryDto[]>("/sales/orders?take=500"),
    backendFetchJson<DispatchSummaryDto[]>("/sales/dispatches?take=500"),
    backendFetchJson<DirectDispatchSummaryDto[]>("/sales/direct-dispatches?take=500").catch(
      () => [] as DirectDispatchSummaryDto[],
    ),
  ]);

  const customerById = new Map(customers.map((c) => [c.id, c]));
  const orderById = new Map(orders.map((o) => [o.id, o]));
  const postedDispatches = dispatches
    .filter((dispatch) => dispatch.status === 1 && dispatch.lineCount > 0)
    .map((dispatch) => ({
      id: dispatch.id,
      number: dispatch.number,
      customerId: orderById.get(dispatch.salesOrderId)?.customerId ?? "",
      dispatchedAt: dispatch.dispatchedAt,
      lineCount: dispatch.lineCount,
    }))
    .filter((dispatch) => dispatch.customerId);
  const postedDirectDispatches = directDispatches
    .filter((dispatch) => dispatch.status === 1 && dispatch.lineCount > 0 && dispatch.customerId)
    .map((dispatch) => ({
      id: dispatch.id,
      number: dispatch.number,
      customerId: dispatch.customerId ?? "",
      dispatchedAt: dispatch.dispatchedAt,
      lineCount: dispatch.lineCount,
    }));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Final Invoices</h1>
        <p className="mt-1 text-sm text-zinc-500">Draft -&gt; add lines -&gt; post -&gt; pay (via payments).</p>
      </div>

      {canManageInvoices ? (
        <AppFormModal title="Create Final Invoice" description="Create a draft final invoice from customer, dispatch, or direct dispatch details." buttonLabel="+ New Invoice" size="xl">
          <InvoiceCreateForm
            customers={customers}
            dispatches={postedDispatches}
            directDispatches={postedDirectDispatches}
          />
        </AppFormModal>
      ) : null}

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <SearchableTable placeholder="Search invoices..." emptyMessage="No invoices yet." emptyColSpan={7} headers={
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Customer</th>
                <th className="py-2 pr-3">Invoice Date</th>
                <th className="py-2 pr-3">Due Date</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Total</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
          }>
              {invoices.map((i) => {
                const customer = customerById.get(i.customerId)?.code ?? i.customerId;
                const status = statusLabel[i.status] ?? String(i.status);
                return (
                <SearchableRow key={i.id} searchText={[i.number, customer, status, i.total].join(" ")}>
                <tr key={i.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/sales/invoices/${i.id}`}>
                      {i.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">{customer}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(i.invoiceDate).toLocaleString()}</td>
                  <td className="py-2 pr-3 text-zinc-500">
                    {i.dueDate ? new Date(i.dueDate).toLocaleDateString() : "-"}
                  </td>
                  <td className="py-2 pr-3">{status}</td>
                  <td className="py-2 pr-3">{i.total}</td>
                  <td className="py-2 pr-3">
                    <ListViewEditActions
                      viewHref={`/sales/invoices/${i.id}`}
                      canEdit={canManageInvoices && i.status === 0}
                      auditTableName="SalesInvoices"
                      auditRecordId={i.id}
                    />
                  </td>
                </tr>
                </SearchableRow>
                );
              })}
        </SearchableTable>
      </Card>
    </div>
  );
}
