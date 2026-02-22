import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { PaymentCreateForm } from "./PaymentCreateForm";

type PaymentDto = {
  id: string;
  referenceNumber: string;
  direction: number;
  counterpartyType: number;
  counterpartyId: string;
  amount: number;
  paidAt: string;
  notes?: string | null;
};

type CustomerDto = { id: string; code: string; name: string };
type SupplierDto = { id: string; code: string; name: string };

const directionLabel: Record<number, string> = { 1: "Incoming", 2: "Outgoing" };
const counterpartyLabel: Record<number, string> = { 1: "Customer", 2: "Supplier" };

export default async function PaymentsPage() {
  const [payments, customers, suppliers] = await Promise.all([
    backendFetchJson<PaymentDto[]>("/finance/payments?take=100"),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<SupplierDto[]>("/suppliers"),
  ]);

  const customerById = new Map(customers.map((c) => [c.id, c]));
  const supplierById = new Map(suppliers.map((s) => [s.id, s]));

  function counterpartyCode(type: number, id: string): string {
    if (type === 1) return customerById.get(id)?.code ?? id;
    if (type === 2) return supplierById.get(id)?.code ?? id;
    return id;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Payments</h1>
        <p className="mt-1 text-sm text-zinc-500">Record incoming/outgoing payments and allocate to AR/AP.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <PaymentCreateForm customers={customers} suppliers={suppliers} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Reference</th>
                <th className="py-2 pr-3">Direction</th>
                <th className="py-2 pr-3">Counterparty</th>
                <th className="py-2 pr-3">Paid</th>
                <th className="py-2 pr-3">Amount</th>
                <th className="py-2 pr-3">Notes</th>
              </tr>
            </thead>
            <tbody>
              {payments.map((p) => (
                <tr key={p.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/finance/payments/${p.id}`}>
                      {p.referenceNumber}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">{directionLabel[p.direction] ?? p.direction}</td>
                  <td className="py-2 pr-3">
                    {counterpartyLabel[p.counterpartyType] ?? p.counterpartyType}:{" "}
                    {counterpartyCode(p.counterpartyType, p.counterpartyId)}
                  </td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(p.paidAt).toLocaleString()}</td>
                  <td className="py-2 pr-3">{p.amount}</td>
                  <td className="py-2 pr-3 text-zinc-500">{p.notes ?? "—"}</td>
                </tr>
              ))}
              {payments.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={6}>
                    No payments yet.
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

