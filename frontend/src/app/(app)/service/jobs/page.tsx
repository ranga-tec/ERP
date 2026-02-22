import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { ServiceJobCreateForm } from "./ServiceJobCreateForm";

type ServiceJobDto = {
  id: string;
  number: string;
  equipmentUnitId: string;
  customerId: string;
  openedAt: string;
  problemDescription: string;
  status: number;
  completedAt?: string | null;
};

type EquipmentUnitDto = { id: string; serialNumber: string; itemId: string; customerId: string };
type CustomerDto = { id: string; code: string; name: string };

const statusLabel: Record<number, string> = {
  0: "Open",
  1: "In Progress",
  2: "Completed",
  3: "Closed",
  4: "Cancelled",
};

export default async function ServiceJobsPage() {
  const [jobs, units, customers] = await Promise.all([
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=100"),
    backendFetchJson<EquipmentUnitDto[]>("/service/equipment-units?take=500"),
    backendFetchJson<CustomerDto[]>("/customers"),
  ]);

  const unitById = new Map(units.map((u) => [u.id, u]));
  const customerById = new Map(customers.map((c) => [c.id, c]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Service Jobs</h1>
        <p className="mt-1 text-sm text-zinc-500">Open → in progress → completed → closed.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <ServiceJobCreateForm equipmentUnits={units} customers={customers} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Equipment</th>
                <th className="py-2 pr-3">Customer</th>
                <th className="py-2 pr-3">Opened</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Completed</th>
              </tr>
            </thead>
            <tbody>
              {jobs.map((j) => (
                <tr key={j.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/service/jobs/${j.id}`}>
                      {j.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-600 dark:text-zinc-400">
                    {unitById.get(j.equipmentUnitId)?.serialNumber ?? j.equipmentUnitId}
                  </td>
                  <td className="py-2 pr-3">{customerById.get(j.customerId)?.code ?? j.customerId}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(j.openedAt).toLocaleString()}</td>
                  <td className="py-2 pr-3">{statusLabel[j.status] ?? j.status}</td>
                  <td className="py-2 pr-3 text-zinc-500">
                    {j.completedAt ? new Date(j.completedAt).toLocaleString() : "—"}
                  </td>
                </tr>
              ))}
              {jobs.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={6}>
                    No service jobs yet.
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

