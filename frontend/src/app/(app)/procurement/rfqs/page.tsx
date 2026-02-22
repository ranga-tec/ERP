import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { RfqCreateForm } from "./RfqCreateForm";

type SupplierDto = { id: string; code: string; name: string };
type RfqSummaryDto = {
  id: string;
  number: string;
  supplierId: string;
  requestedAt: string;
  status: number;
  lineCount: number;
};

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Sent",
  2: "Closed",
  3: "Cancelled",
};

export default async function RfqsPage() {
  const [rfqs, suppliers] = await Promise.all([
    backendFetchJson<RfqSummaryDto[]>("/procurement/rfqs?take=100"),
    backendFetchJson<SupplierDto[]>("/suppliers"),
  ]);

  const supplierById = new Map(suppliers.map((s) => [s.id, s]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">RFQs</h1>
        <p className="mt-1 text-sm text-zinc-500">Request for quotation workflow.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <RfqCreateForm suppliers={suppliers} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Supplier</th>
                <th className="py-2 pr-3">Requested</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Lines</th>
              </tr>
            </thead>
            <tbody>
              {rfqs.map((r) => (
                <tr key={r.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/procurement/rfqs/${r.id}`}>
                      {r.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">
                    {supplierById.get(r.supplierId)?.code ?? r.supplierId}
                  </td>
                  <td className="py-2 pr-3 text-zinc-500">
                    {new Date(r.requestedAt).toLocaleString()}
                  </td>
                  <td className="py-2 pr-3">{statusLabel[r.status] ?? r.status}</td>
                  <td className="py-2 pr-3">{r.lineCount}</td>
                </tr>
              ))}
              {rfqs.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={5}>
                    No RFQs yet.
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

