import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { PaymentTypeCreateForm } from "./PaymentTypeCreateForm";

type PaymentTypeDto = {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
};

export default async function PaymentTypesPage() {
  const paymentTypes = await backendFetchJson<PaymentTypeDto[]>("/payment-types");

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Payment Types</h1>
        <p className="mt-1 text-sm text-zinc-500">Master list for cash/bank/card and other payment methods.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <PaymentTypeCreateForm />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Code</th>
                <th className="py-2 pr-3">Name</th>
                <th className="py-2 pr-3">Description</th>
                <th className="py-2 pr-3">Active</th>
              </tr>
            </thead>
            <tbody>
              {paymentTypes.map((p) => (
                <tr key={p.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">{p.code}</td>
                  <td className="py-2 pr-3">{p.name}</td>
                  <td className="py-2 pr-3 text-zinc-500">{p.description ?? "-"}</td>
                  <td className="py-2 pr-3">{p.isActive ? "Yes" : "No"}</td>
                </tr>
              ))}
              {paymentTypes.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={4}>
                    No payment types yet.
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
