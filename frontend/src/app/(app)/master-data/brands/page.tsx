import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { TableSearchInput } from "@/components/TableSearchInput";
import { Card, Table } from "@/components/ui";
import { BrandCreateForm } from "./BrandCreateForm";
import { BrandRow } from "./BrandRow";

type BrandDto = { id: string; code: string; name: string; isActive: boolean };

export default async function BrandsPage() {
  const brands = await backendFetchJson<BrandDto[]>("/brands");

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Brands</h1>
          <p className="mt-1 text-sm text-zinc-500">Master data for item brands.</p>
        </div>
        <AppFormModal title="Create Brand" description="Add a new item brand." buttonLabel="+ New Brand">
          <BrandCreateForm />
        </AppFormModal>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <TableSearchInput placeholder="Search brands..." />
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Code</th>
                <th className="py-2 pr-3">Name</th>
                <th className="py-2 pr-3">Active</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {brands.map((b) => <BrandRow key={b.id} brand={b} />)}
              {brands.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={4}>
                    No brands yet.
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
