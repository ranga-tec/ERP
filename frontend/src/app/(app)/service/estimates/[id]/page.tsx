import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, SecondaryLink, Table } from "@/components/ui";
import { ServiceEstimateActions } from "../ServiceEstimateActions";
import { ServiceEstimateLineAddForm } from "../ServiceEstimateLineAddForm";

type ServiceEstimateDto = {
  id: string;
  number: string;
  serviceJobId: string;
  issuedAt: string;
  validUntil?: string | null;
  terms?: string | null;
  status: number;
  subtotal: number;
  taxTotal: number;
  total: number;
  lines: {
    id: string;
    kind: number;
    itemId?: string | null;
    description: string;
    quantity: number;
    unitPrice: number;
    taxPercent: number;
    lineSubtotal: number;
    lineTax: number;
    lineTotal: number;
  }[];
};

type ServiceJobDto = { id: string; number: string; customerId: string; status: number };
type CustomerDto = { id: string; code: string; name: string };
type ItemDto = { id: string; sku: string; name: string; defaultUnitCost: number };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Approved",
  2: "Rejected",
};

const kindLabel: Record<number, string> = {
  1: "Part",
  2: "Labor",
};

export default async function ServiceEstimateDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [estimate, jobs, customers, items] = await Promise.all([
    backendFetchJson<ServiceEstimateDto>(`/service/estimates/${id}`),
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=500"),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<ItemDto[]>("/items"),
  ]);

  const jobById = new Map(jobs.map((j) => [j.id, j]));
  const customerById = new Map(customers.map((c) => [c.id, c]));
  const itemById = new Map(items.map((i) => [i.id, i]));
  const job = jobById.get(estimate.serviceJobId);
  const customer = job ? customerById.get(job.customerId) : null;
  const isDraft = estimate.status === 0;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/service/estimates" className="hover:underline">
            Service Estimates
          </Link>{" "}
          / <span className="font-mono text-xs">{estimate.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Estimate {estimate.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>
            Job: <span className="font-mono text-xs">{job?.number ?? estimate.serviceJobId}</span>
          </div>
          <div>Customer: {customer ? customer.code : "-"}</div>
          <div>Status: {statusLabel[estimate.status] ?? estimate.status}</div>
          <div>Issued: {new Date(estimate.issuedAt).toLocaleString()}</div>
          <div>Valid till: {estimate.validUntil ? new Date(estimate.validUntil).toLocaleString() : "-"}</div>
        </div>
        <div className="mt-2 text-sm text-zinc-500">
          Subtotal: {estimate.subtotal.toFixed(2)} · Tax: {estimate.taxTotal.toFixed(2)} · Total:{" "}
          {estimate.total.toFixed(2)}
        </div>
      </div>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink href={`/api/backend/service/estimates/${estimate.id}/pdf`} target="_blank" rel="noopener noreferrer">
            Download PDF
          </SecondaryLink>
        </div>
        <ServiceEstimateActions
          estimateId={estimate.id}
          canApprove={isDraft && estimate.lines.length > 0}
          canReject={isDraft}
          canSend={estimate.status !== 2 && estimate.lines.length > 0}
        />
      </Card>

      {isDraft ? (
        <Card>
          <div className="mb-3 text-sm font-semibold">Add line</div>
          <ServiceEstimateLineAddForm estimateId={estimate.id} items={items} />
        </Card>
      ) : null}

      {estimate.terms ? (
        <Card>
          <div className="mb-2 text-sm font-semibold">Terms</div>
          <div className="whitespace-pre-wrap text-sm text-zinc-700 dark:text-zinc-200">{estimate.terms}</div>
        </Card>
      ) : null}

      <Card>
        <div className="mb-3 text-sm font-semibold">Lines</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Kind</th>
                <th className="py-2 pr-3">Item</th>
                <th className="py-2 pr-3">Description</th>
                <th className="py-2 pr-3">Qty / Hrs</th>
                <th className="py-2 pr-3">Unit Price</th>
                <th className="py-2 pr-3">Tax %</th>
                <th className="py-2 pr-3">Line Total</th>
              </tr>
            </thead>
            <tbody>
              {estimate.lines.map((line) => (
                <tr key={line.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3">{kindLabel[line.kind] ?? line.kind}</td>
                  <td className="py-2 pr-3">{line.itemId ? itemById.get(line.itemId)?.sku ?? line.itemId : "-"}</td>
                  <td className="py-2 pr-3 text-zinc-500">{line.description}</td>
                  <td className="py-2 pr-3">{line.quantity}</td>
                  <td className="py-2 pr-3">{line.unitPrice.toFixed(2)}</td>
                  <td className="py-2 pr-3">{line.taxPercent.toFixed(2)}</td>
                  <td className="py-2 pr-3">{line.lineTotal.toFixed(2)}</td>
                </tr>
              ))}
              {estimate.lines.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={7}>
                    No lines yet.
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
