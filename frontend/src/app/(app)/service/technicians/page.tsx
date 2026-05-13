import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { ServiceTechnicianCreateForm } from "./ServiceTechnicianCreateForm";
import { ServiceTechnicianRow } from "./ServiceTechnicianRow";

type ServiceTechnicianDto = {
  id: string;
  code: string;
  name: string;
  defaultCostRate: number;
  defaultBillingRate: number;
  phone?: string | null;
  notes?: string | null;
  isActive: boolean;
};

export default async function ServiceTechniciansPage() {
  const technicians = await backendFetchJson<ServiceTechnicianDto[]>("/service/technicians");

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Technicians</h1>
        <p className="mt-1 text-sm text-zinc-500">Maintain service technicians used on job detail labor entries.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <ServiceTechnicianCreateForm />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Code</th>
                <th className="py-2 pr-3">Name</th>
                <th className="py-2 pr-3">Cost Rate</th>
                <th className="py-2 pr-3">Billing Rate</th>
                <th className="py-2 pr-3">Phone</th>
                <th className="py-2 pr-3">Notes</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {technicians.map((technician) => (
                <ServiceTechnicianRow key={technician.id} technician={technician} />
              ))}
              {technicians.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={8}>
                    No technicians yet.
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
