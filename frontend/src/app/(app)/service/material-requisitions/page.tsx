import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { MaterialRequisitionCreateForm } from "./MaterialRequisitionCreateForm";

type MaterialRequisitionSummaryDto = {
  id: string;
  number: string;
  serviceJobId: string;
  warehouseId: string;
  requestedAt: string;
  status: number;
  lineCount: number;
};

type ServiceJobDto = { id: string; number: string };
type WarehouseDto = { id: string; code: string; name: string };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Voided",
};

export default async function MaterialRequisitionsPage() {
  const [mrs, jobs, warehouses] = await Promise.all([
    backendFetchJson<MaterialRequisitionSummaryDto[]>("/service/material-requisitions?take=100"),
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=500"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
  ]);

  const jobById = new Map(jobs.map((j) => [j.id, j]));
  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Material Requisitions</h1>
        <p className="mt-1 text-sm text-zinc-500">Issue materials to a service job from a warehouse.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <MaterialRequisitionCreateForm serviceJobs={jobs} warehouses={warehouses} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Job</th>
                <th className="py-2 pr-3">Warehouse</th>
                <th className="py-2 pr-3">Requested</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Lines</th>
              </tr>
            </thead>
            <tbody>
              {mrs.map((m) => (
                <tr key={m.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/service/material-requisitions/${m.id}`}>
                      {m.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-600 dark:text-zinc-400">
                    {jobById.get(m.serviceJobId)?.number ?? m.serviceJobId}
                  </td>
                  <td className="py-2 pr-3">{warehouseById.get(m.warehouseId)?.code ?? m.warehouseId}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(m.requestedAt).toLocaleString()}</td>
                  <td className="py-2 pr-3">{statusLabel[m.status] ?? m.status}</td>
                  <td className="py-2 pr-3">{m.lineCount}</td>
                </tr>
              ))}
              {mrs.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={6}>
                    No material requisitions yet.
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

