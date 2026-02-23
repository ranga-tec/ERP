import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { DirectDispatchCreateForm } from "./DirectDispatchCreateForm";

type DirectDispatchSummaryDto = {
  id: string;
  number: string;
  warehouseId: string;
  customerId?: string | null;
  serviceJobId?: string | null;
  dispatchedAt: string;
  status: number;
  reason?: string | null;
  lineCount: number;
};

type CustomerDto = { id: string; code: string; name: string };
type ServiceJobDto = { id: string; number: string; customerId: string };
type WarehouseDto = { id: string; code: string; name: string };

const statusLabel: Record<number, string> = { 0: "Draft", 1: "Posted", 2: "Voided" };

export default async function DirectDispatchesPage() {
  const [rows, customers, jobs, warehouses] = await Promise.all([
    backendFetchJson<DirectDispatchSummaryDto[]>("/sales/direct-dispatches?take=100"),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=200"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
  ]);

  const customerById = new Map(customers.map((c) => [c.id, c]));
  const jobById = new Map(jobs.map((j) => [j.id, j]));
  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Direct Dispatches</h1>
        <p className="mt-1 text-sm text-zinc-500">Immediate stock issue without a sales order (sales/service scenarios).</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <DirectDispatchCreateForm customers={customers} serviceJobs={jobs} warehouses={warehouses} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Customer</th>
                <th className="py-2 pr-3">Service Job</th>
                <th className="py-2 pr-3">Warehouse</th>
                <th className="py-2 pr-3">Date</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Lines</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => (
                <tr key={r.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/sales/direct-dispatches/${r.id}`}>
                      {r.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">{r.customerId ? customerById.get(r.customerId)?.code ?? r.customerId : "-"}</td>
                  <td className="py-2 pr-3 font-mono text-xs">{r.serviceJobId ? jobById.get(r.serviceJobId)?.number ?? r.serviceJobId : "-"}</td>
                  <td className="py-2 pr-3">{warehouseById.get(r.warehouseId)?.code ?? r.warehouseId}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(r.dispatchedAt).toLocaleString()}</td>
                  <td className="py-2 pr-3">{statusLabel[r.status] ?? r.status}</td>
                  <td className="py-2 pr-3">{r.lineCount}</td>
                </tr>
              ))}
              {rows.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={7}>
                    No direct dispatches yet.
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
