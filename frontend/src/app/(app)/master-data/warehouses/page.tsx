import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { TableSearchInput } from "@/components/TableSearchInput";
import { Card, Table } from "@/components/ui";
import { WarehouseCreateForm } from "./WarehouseCreateForm";
import { WarehouseRow } from "./WarehouseRow";

type WarehouseDto = {
  id: string;
  code: string;
  name: string;
  address?: string | null;
  isActive: boolean;
};

export default async function WarehousesPage() {
  const warehouses = await backendFetchJson<WarehouseDto[]>("/warehouses");

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Warehouses</h1>
          <p className="mt-1 text-sm text-zinc-500">Maintain warehouse headers used by inventory, procurement, sales, and service documents.</p>
        </div>
        <AppFormModal title="Create Warehouse" description="Add a warehouse header." buttonLabel="+ New Warehouse">
          <WarehouseCreateForm />
        </AppFormModal>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <TableSearchInput placeholder="Search warehouses..." />
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Code</th>
                <th className="py-2 pr-3">Name</th>
                <th className="py-2 pr-3">Address</th>
                <th className="py-2 pr-3">Active</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {warehouses.map((warehouse) => (
                <WarehouseRow key={warehouse.id} warehouse={warehouse} />
              ))}
              {warehouses.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={5}>
                    No warehouses yet.
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
