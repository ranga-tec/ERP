import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { TransactionLink } from "@/components/TransactionLink";
import { Card, Table } from "@/components/ui";
import { DirectPurchaseCreateForm } from "./DirectPurchaseCreateForm";

type DirectPurchaseSummaryDto = {
  id: string;
  number: string;
  supplierId: string;
  warehouseId: string;
  serviceJobId?: string | null;
  purchasedAt: string;
  status: number;
  remarks?: string | null;
  subtotal: number;
  taxTotal: number;
  grandTotal: number;
};

type SupplierDto = { id: string; code: string; name: string };
type WarehouseDto = { id: string; code: string; name: string };
type ServiceJobDto = { id: string; number: string; kind: number };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Voided",
};

export default async function DirectPurchasesPage() {
  const [rows, suppliers, warehouses, serviceJobs] = await Promise.all([
    backendFetchJson<DirectPurchaseSummaryDto[]>("/procurement/direct-purchases?take=100"),
    backendFetchJson<SupplierDto[]>("/suppliers"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=500"),
  ]);

  const supplierById = new Map(suppliers.map((s) => [s.id, s]));
  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));
  const jobById = new Map(serviceJobs.map((job) => [job.id, job]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Direct Purchases</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Receive purchases without a PO. Link emergency outside buys to the relevant service job when needed.
        </p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <DirectPurchaseCreateForm suppliers={suppliers} warehouses={warehouses} serviceJobs={serviceJobs} />
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
                <th className="py-2 pr-3">Service Job</th>
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
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-600 dark:text-zinc-400">
                    {r.serviceJobId ? (
                      <TransactionLink referenceType="SJ" referenceId={r.serviceJobId} monospace>
                        {jobById.get(r.serviceJobId)?.number ?? r.serviceJobId}
                      </TransactionLink>
                    ) : (
                      "-"
                    )}
                  </td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(r.purchasedAt).toLocaleString()}</td>
                  <td className="py-2 pr-3">{statusLabel[r.status] ?? r.status}</td>
                  <td className="py-2 pr-3">{r.grandTotal.toFixed(2)}</td>
                  <td className="py-2 pr-3 text-zinc-500">{r.remarks ?? "-"}</td>
                </tr>
              ))}
              {rows.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={8}>
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
