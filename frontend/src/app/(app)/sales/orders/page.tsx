import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
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
      <div>
        <h1 className="text-2xl font-semibold">Sales Orders</h1>
        <p className="mt-1 text-sm text-zinc-500">Draft → add lines → confirm → dispatch.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <SalesOrderCreateForm customers={customers} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Customer</th>
                <th className="py-2 pr-3">Order Date</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Total</th>
              </tr>
            </thead>
            <tbody>
              {orders.map((o) => (
                <tr key={o.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/sales/orders/${o.id}`}>
                      {o.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">{customerById.get(o.customerId)?.code ?? o.customerId}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(o.orderDate).toLocaleString()}</td>
                  <td className="py-2 pr-3">{statusLabel[o.status] ?? o.status}</td>
                  <td className="py-2 pr-3">{o.total}</td>
                </tr>
              ))}
              {orders.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={5}>
                    No sales orders yet.
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

