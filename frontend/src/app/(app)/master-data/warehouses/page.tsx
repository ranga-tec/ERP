import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { WarehouseCreateForm } from "./WarehouseCreateForm";

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
      <div>
        <h1 className="text-2xl font-semibold">Warehouses</h1>
        <p className="mt-1 text-sm text-zinc-500">Multi-warehouse master data.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <WarehouseCreateForm />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Code</th>
                <th className="py-2 pr-3">Name</th>
                <th className="py-2 pr-3">Address</th>
                <th className="py-2 pr-3">Active</th>
              </tr>
            </thead>
            <tbody>
              {warehouses.map((w) => (
                <tr key={w.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">{w.code}</td>
                  <td className="py-2 pr-3">{w.name}</td>
                  <td className="py-2 pr-3 text-zinc-500">{w.address ?? "—"}</td>
                  <td className="py-2 pr-3">{w.isActive ? "Yes" : "No"}</td>
                </tr>
              ))}
              {warehouses.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={4}>
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

