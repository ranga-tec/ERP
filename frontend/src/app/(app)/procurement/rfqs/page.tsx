import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { ListViewEditActions } from "@/components/ListViewEditActions";
import { SearchableRow, SearchableTable } from "@/components/SearchableTable";
import { Card } from "@/components/ui";
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
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">RFQs</h1>
          <p className="mt-1 text-sm text-zinc-500">Request for quotation workflow.</p>
        </div>
        <AppFormModal title="Create RFQ" description="Create a request for quotation." buttonLabel="+ New RFQ">
          <RfqCreateForm suppliers={suppliers} />
        </AppFormModal>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <SearchableTable placeholder="Search RFQs..." emptyMessage="No RFQs yet." emptyColSpan={6} headers={
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Supplier</th>
                <th className="py-2 pr-3">Requested</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Lines</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
          }>
              {rfqs.map((r) => {
                const supplier = supplierById.get(r.supplierId)?.code ?? r.supplierId;
                const status = statusLabel[r.status] ?? String(r.status);
                return (
                <SearchableRow key={r.id} searchText={[r.number, supplier, status, r.lineCount].join(" ")}>
                <tr key={r.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/procurement/rfqs/${r.id}`}>
                      {r.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">
                    {supplier}
                  </td>
                  <td className="py-2 pr-3 text-zinc-500">
                    {new Date(r.requestedAt).toLocaleString()}
                  </td>
                  <td className="py-2 pr-3">{status}</td>
                  <td className="py-2 pr-3">{r.lineCount}</td>
                  <td className="py-2 pr-3">
                    <ListViewEditActions
                      viewHref={`/procurement/rfqs/${r.id}`}
                      canEdit={r.status === 0}
                      editInModal
                      editModalTitle={`Edit RFQ ${r.number}`}
                      auditTableName="Rfqs"
                      auditRecordId={r.id}
                    />
                  </td>
                </tr>
                </SearchableRow>
                );
              })}
        </SearchableTable>
      </Card>
    </div>
  );
}
