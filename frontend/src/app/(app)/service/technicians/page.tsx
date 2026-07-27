import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { SearchableRow, SearchableTable } from "@/components/SearchableTable";
import { Card } from "@/components/ui";
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
  userId?: string | null;
};

export default async function ServiceTechniciansPage() {
  const technicians = await backendFetchJson<ServiceTechnicianDto[]>("/service/technicians");

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Technicians</h1>
          <p className="mt-1 text-sm text-zinc-500">
            Technicians used on job detail labour entries. Staff technicians are created from
            Admin &rsaquo; Users (open the user and use &ldquo;Technician profile&rdquo;) so nobody is
            entered twice &mdash; add them here only when they have no system login, such as a
            subcontractor.
          </p>
        </div>
        <AppFormModal title="Create Technician" description="For technicians with no system login, such as subcontractors. Staff technicians are created from Admin > Users." buttonLabel="+ New Technician">
          <ServiceTechnicianCreateForm />
        </AppFormModal>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <SearchableTable
          placeholder="Search technician code, name, phone, notes..."
          emptyMessage="No technicians yet."
          emptyColSpan={9}
          headers={
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Code</th>
                <th className="py-2 pr-3">Name</th>
                <th className="py-2 pr-3">Cost Rate</th>
                <th className="py-2 pr-3">Billing Rate</th>
                <th className="py-2 pr-3">Source</th>
                <th className="py-2 pr-3">Phone</th>
                <th className="py-2 pr-3">Notes</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
          }
        >
              {technicians.map((technician) => (
                <SearchableRow
                  key={technician.id}
                  searchText={[
                    technician.code,
                    technician.name,
                    technician.phone,
                    technician.notes,
                    technician.isActive ? "active" : "inactive",
                    technician.userId ? "staff login" : "subcontractor",
                  ].filter(Boolean).join(" ")}
                >
                <ServiceTechnicianRow key={technician.id} technician={technician} />
                </SearchableRow>
              ))}
        </SearchableTable>
      </Card>
    </div>
  );
}
