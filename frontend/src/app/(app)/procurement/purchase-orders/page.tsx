import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { PurchaseOrderCreateForm } from "./PurchaseOrderCreateForm";

type SupplierDto = { id: string; code: string; name: string };
type PurchaseOrderSummaryDto = {
  id: string;
  number: string;
  supplierId: string;
  orderDate: string;
  status: number;
  total: number;
};

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Approved",
  2: "Partially Received",
  3: "Closed",
  4: "Cancelled",
};

export default async function PurchaseOrdersPage() {
  const [pos, suppliers] = await Promise.all([
    backendFetchJson<PurchaseOrderSummaryDto[]>("/procurement/purchase-orders?take=100"),
    backendFetchJson<SupplierDto[]>("/suppliers"),
  ]);

  const supplierById = new Map(suppliers.map((s) => [s.id, s]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Purchase Orders</h1>
        <p className="mt-1 text-sm text-zinc-500">PO workflow: draft → approve → receive.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <PurchaseOrderCreateForm suppliers={suppliers} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Supplier</th>
                <th className="py-2 pr-3">Order Date</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Total</th>
              </tr>
            </thead>
            <tbody>
              {pos.map((p) => (
                <tr key={p.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/procurement/purchase-orders/${p.id}`}>
                      {p.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">{supplierById.get(p.supplierId)?.code ?? p.supplierId}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(p.orderDate).toLocaleString()}</td>
                  <td className="py-2 pr-3">{statusLabel[p.status] ?? p.status}</td>
                  <td className="py-2 pr-3">{p.total}</td>
                </tr>
              ))}
              {pos.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={5}>
                    No purchase orders yet.
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

