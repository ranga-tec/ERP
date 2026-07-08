import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { ListViewEditActions } from "@/components/ListViewEditActions";
import { SearchableRow, SearchableTable } from "@/components/SearchableTable";
import { Card } from "@/components/ui";
import { SalesOrderCreateForm } from "./SalesOrderCreateForm";

type CustomerDto = { id: string; code: string; name: string };
type SalesOrderSummaryDto = {
  id: string;
  number: string;
  customerId: string;
  orderDate: string;
  status: number;
  total: number;
};

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Confirmed",
  2: "Fulfilled",
  3: "Closed",
  4: "Cancelled",
};

export default async function SalesOrdersPage() {
  const [orders, customers] = await Promise.all([
    backendFetchJson<SalesOrderSummaryDto[]>("/sales/orders?take=100"),
    backendFetchJson<CustomerDto[]>("/customers"),
  ]);

  const customerById = new Map(customers.map((c) => [c.id, c]));

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Sales Orders</h1>
          <p className="mt-1 text-sm text-zinc-500">Draft -&gt; add lines -&gt; confirm -&gt; dispatch.</p>
        </div>
        <AppFormModal title="Create Sales Order" description="Create a draft order before adding lines." buttonLabel="+ New Order">
          <SalesOrderCreateForm customers={customers} />
        </AppFormModal>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <SearchableTable placeholder="Search orders..." emptyMessage="No sales orders yet." emptyColSpan={6} headers={
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Customer</th>
                <th className="py-2 pr-3">Order Date</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Total</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
          }>
              {orders.map((o) => {
                const customer = customerById.get(o.customerId)?.code ?? o.customerId;
                const status = statusLabel[o.status] ?? String(o.status);
                return (
                <SearchableRow key={o.id} searchText={[o.number, customer, status, o.total].join(" ")}>
                <tr key={o.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/sales/orders/${o.id}`}>
                      {o.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">{customer}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(o.orderDate).toLocaleString()}</td>
                  <td className="py-2 pr-3">{status}</td>
                  <td className="py-2 pr-3">{o.total}</td>
                  <td className="py-2 pr-3">
                    <ListViewEditActions
                      viewHref={`/sales/orders/${o.id}`}
                      canEdit={o.status === 0}
                      editInModal
                      editModalTitle={`Edit Order ${o.number}`}
                      auditTableName="SalesOrders"
                      auditRecordId={o.id}
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
