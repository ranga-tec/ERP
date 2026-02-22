import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { PurchaseRequisitionCreateForm } from "./PurchaseRequisitionCreateForm";

type PurchaseRequisitionSummaryDto = {
  id: string;
  number: string;
  requestDate: string;
  status: number;
  lineCount: number;
  notes?: string | null;
};

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Submitted",
  2: "Approved",
  3: "Rejected",
  4: "Cancelled",
};

export default async function PurchaseRequisitionsPage() {
  const prs = await backendFetchJson<PurchaseRequisitionSummaryDto[]>(
    "/procurement/purchase-requisitions?take=100",
  );

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Purchase Requisitions</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Internal purchase requests before RFQ/PO processing.
        </p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <PurchaseRequisitionCreateForm />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Request Date</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Lines</th>
                <th className="py-2 pr-3">Notes</th>
              </tr>
            </thead>
            <tbody>
              {prs.map((pr) => (
                <tr key={pr.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link href={`/procurement/purchase-requisitions/${pr.id}`} className="hover:underline">
                      {pr.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3 text-zinc-500">
                    {new Date(pr.requestDate).toLocaleString()}
                  </td>
                  <td className="py-2 pr-3">{statusLabel[pr.status] ?? pr.status}</td>
                  <td className="py-2 pr-3">{pr.lineCount}</td>
                  <td className="py-2 pr-3 text-zinc-500">{pr.notes ?? "-"}</td>
                </tr>
              ))}
              {prs.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={5}>
                    No purchase requisitions yet.
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
