import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { CustomerCreateForm } from "./CustomerCreateForm";

type CustomerDto = {
  id: string;
  code: string;
  name: string;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  isActive: boolean;
};

export default async function CustomersPage() {
  const customers = await backendFetchJson<CustomerDto[]>("/customers");

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Customers</h1>
        <p className="mt-1 text-sm text-zinc-500">Customer master data.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <CustomerCreateForm />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Code</th>
                <th className="py-2 pr-3">Name</th>
                <th className="py-2 pr-3">Phone</th>
                <th className="py-2 pr-3">Email</th>
                <th className="py-2 pr-3">Active</th>
              </tr>
            </thead>
            <tbody>
              {customers.map((c) => (
                <tr key={c.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">{c.code}</td>
                  <td className="py-2 pr-3">{c.name}</td>
                  <td className="py-2 pr-3 text-zinc-500">{c.phone ?? "—"}</td>
                  <td className="py-2 pr-3 text-zinc-500">{c.email ?? "—"}</td>
                  <td className="py-2 pr-3">{c.isActive ? "Yes" : "No"}</td>
                </tr>
              ))}
              {customers.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={5}>
                    No customers yet.
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

