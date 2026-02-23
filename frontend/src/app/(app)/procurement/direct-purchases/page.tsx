import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { DirectPurchaseCreateForm } from "./DirectPurchaseCreateForm";

type DirectPurchaseSummaryDto = {
  id: string;
  number: string;
  supplierId: string;
  warehouseId: string;
  purchasedAt: string;
  status: number;
  remarks?: string | null;
  subtotal: number;
  taxTotal: number;
  grandTotal: number;
};

type SupplierDto = { id: string; code: string; name: string };
type WarehouseDto = { id: string; code: string; name: string };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Voided",
};

export default async function DirectPurchasesPage() {
  const [rows, suppliers, warehouses] = await Promise.all([
    backendFetchJson<DirectPurchaseSummaryDto[]>("/procurement/direct-purchases?take=100"),
    backendFetchJson<SupplierDto[]>("/suppliers"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
  ]);

  const supplierById = new Map(suppliers.map((s) => [s.id, s]));
  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Direct Purchases</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Receive purchases without a PO. Post to stock first, then create a supplier invoice if needed.
        </p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <DirectPurchaseCreateForm suppliers={suppliers} warehouses={warehouses} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Supplier</th>
                <th className="py-2 pr-3">Warehouse</th>
                <th className="py-2 pr-3">Date</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Total</th>
                <th className="py-2 pr-3">Remarks</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => (
                <tr key={r.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/procurement/direct-purchases/${r.id}`}>
                      {r.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">{supplierById.get(r.supplierId)?.code ?? r.supplierId}</td>
                  <td className="py-2 pr-3">{warehouseById.get(r.warehouseId)?.code ?? r.warehouseId}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(r.purchasedAt).toLocaleString()}</td>
                  <td className="py-2 pr-3">{statusLabel[r.status] ?? r.status}</td>
                  <td className="py-2 pr-3">{r.grandTotal.toFixed(2)}</td>
                  <td className="py-2 pr-3 text-zinc-500">{r.remarks ?? "-"}</td>
                </tr>
              ))}
              {rows.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={7}>
                    No direct purchases yet.
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
