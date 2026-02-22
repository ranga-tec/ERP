import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { UomCreateForm } from "./UomCreateForm";

type UomDto = { id: string; code: string; name: string; isActive: boolean };

export default async function UomsPage() {
  const uoms = await backendFetchJson<UomDto[]>("/uoms");

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Unit Of Measure</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Master list used for item UoM selection.
        </p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <UomCreateForm />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Code</th>
                <th className="py-2 pr-3">Name</th>
                <th className="py-2 pr-3">Active</th>
              </tr>
            </thead>
            <tbody>
              {uoms.map((u) => (
                <tr key={u.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">{u.code}</td>
                  <td className="py-2 pr-3">{u.name}</td>
                  <td className="py-2 pr-3">{u.isActive ? "Yes" : "No"}</td>
                </tr>
              ))}
              {uoms.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={3}>
                    No UoMs yet.
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
