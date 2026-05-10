import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { PettyCashIouActions } from "./PettyCashIouActions";
import { PettyCashIouCreateForm } from "./PettyCashIouCreateForm";

type ServiceJobDto = { id: string; number: string; status: number };
type FundDto = { id: string; code: string; name: string; isActive: boolean };
type PettyCashIouDto = {
  id: string;
  number: string;
  serviceJobId: string;
  requestedByName: string;
  amount: number;
  purpose: string;
  requestedAt: string;
  expectedSettlementAt?: string | null;
  status: number;
  pettyCashFundId?: string | null;
  settledAmount?: number | null;
};

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Submitted",
  2: "Approved",
  3: "Released",
  4: "Settled",
  5: "Rejected",
  6: "Cancelled",
};

export default async function PettyCashIousPage() {
  const [jobs, funds, ious] = await Promise.all([
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=500"),
    backendFetchJson<FundDto[]>("/finance/petty-cash-funds"),
    backendFetchJson<PettyCashIouDto[]>("/finance/petty-cash-ious?take=200"),
  ]);
  const activeFunds = funds.filter((fund) => fund.isActive);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Petty Cash IOUs</h1>
        <p className="mt-1 text-sm text-zinc-500">Approved cash advances linked to service job numbers.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create IOU</div>
        <PettyCashIouCreateForm serviceJobs={jobs.filter((job) => job.status !== 3 && job.status !== 4)} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">IOUs</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Job</th>
                <th className="py-2 pr-3">Requester</th>
                <th className="py-2 pr-3">Amount</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Purpose</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {ious.map((iou) => (
                <tr key={iou.id} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">{iou.number}</td>
                  <td className="py-2 pr-3">{jobs.find((job) => job.id === iou.serviceJobId)?.number ?? iou.serviceJobId}</td>
                  <td className="py-2 pr-3 text-zinc-500">{iou.requestedByName}</td>
                  <td className="py-2 pr-3">{iou.amount.toFixed(2)}</td>
                  <td className="py-2 pr-3">{statusLabel[iou.status] ?? iou.status}</td>
                  <td className="max-w-sm py-2 pr-3 text-zinc-500">{iou.purpose}</td>
                  <td className="py-2 pr-3">
                    <PettyCashIouActions id={iou.id} status={iou.status} funds={activeFunds} amount={iou.amount} />
                  </td>
                </tr>
              ))}
              {ious.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={7}>No petty cash IOUs yet.</td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>
    </div>
  );
}
