import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { ReferenceFormCreateForm } from "./ReferenceFormCreateForm";

type ReferenceFormDto = {
  id: string;
  code: string;
  name: string;
  module: string;
  routeTemplate?: string | null;
  isActive: boolean;
};

export default async function ReferenceFormsPage() {
  const forms = await backendFetchJson<ReferenceFormDto[]>("/reference-forms");

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Reference Forms</h1>
        <p className="mt-1 text-sm text-zinc-500">Central form code registry for document links and references.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <ReferenceFormCreateForm />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Code</th>
                <th className="py-2 pr-3">Name</th>
                <th className="py-2 pr-3">Module</th>
                <th className="py-2 pr-3">Route</th>
                <th className="py-2 pr-3">Active</th>
              </tr>
            </thead>
            <tbody>
              {forms.map((form) => (
                <tr key={form.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">{form.code}</td>
                  <td className="py-2 pr-3">{form.name}</td>
                  <td className="py-2 pr-3">{form.module}</td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-500">{form.routeTemplate ?? "-"}</td>
                  <td className="py-2 pr-3">{form.isActive ? "Yes" : "No"}</td>
                </tr>
              ))}
              {forms.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={5}>
                    No reference forms yet.
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
