import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Button, Card, Select, Table } from "@/components/ui";
import { buildReferenceRouteMap, resolveReferenceHref } from "@/lib/reference-routing";

type ApDto = {
  id: string;
  supplierId: string;
  referenceType: string;
  referenceId: string;
  amount: number;
  outstanding: number;
  postedAt: string;
};

type SupplierDto = { id: string; code: string; name: string };
type ReferenceFormDto = { code: string; routeTemplate?: string | null; isActive: boolean };

export default async function AccountsPayablePage({ searchParams }: { searchParams?: Promise<{ outstandingOnly?: string }> }) {
  const sp = await searchParams;
  const outstandingOnly = (sp?.outstandingOnly ?? "true") !== "false";

  const [entries, suppliers, referenceForms] = await Promise.all([
    backendFetchJson<ApDto[]>(`/finance/ap?outstandingOnly=${outstandingOnly ? "true" : "false"}`),
    backendFetchJson<SupplierDto[]>("/suppliers"),
    backendFetchJson<ReferenceFormDto[]>("/reference-forms"),
  ]);

  const supplierById = new Map(suppliers.map((s) => [s.id, s]));
  const referenceRouteMap = buildReferenceRouteMap(referenceForms);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Accounts Payable</h1>
        <p className="mt-1 text-sm text-zinc-500">Outstanding supplier balances (typically from posted GRNs).</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Filter</div>
        <form method="GET" className="flex flex-wrap items-end gap-3">
          <div className="min-w-56">
            <label className="mb-1 block text-sm font-medium">Show</label>
            <Select name="outstandingOnly" defaultValue={outstandingOnly ? "true" : "false"}>
              <option value="true">Outstanding only</option>
              <option value="false">All entries</option>
            </Select>
          </div>
          <Button type="submit">Apply</Button>
        </form>
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Entries</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Posted</th>
                <th className="py-2 pr-3">Supplier</th>
                <th className="py-2 pr-3">Ref</th>
                <th className="py-2 pr-3">Amount</th>
                <th className="py-2 pr-3">Outstanding</th>
              </tr>
            </thead>
            <tbody>
              {entries.map((e) => {
                const href = resolveReferenceHref(referenceRouteMap, e.referenceType, e.referenceId);
                return (
                  <tr key={e.id} className="border-b border-zinc-100 dark:border-zinc-900">
                    <td className="py-2 pr-3 text-zinc-500">{new Date(e.postedAt).toLocaleString()}</td>
                    <td className="py-2 pr-3">{supplierById.get(e.supplierId)?.code ?? e.supplierId}</td>
                    <td className="py-2 pr-3 font-mono text-xs">
                      {href ? (
                        <Link className="hover:underline" href={href}>
                          {e.referenceType}:{e.referenceId.slice(0, 8)}
                        </Link>
                      ) : (
                        <span>
                          {e.referenceType}:{e.referenceId.slice(0, 8)}
                        </span>
                      )}
                    </td>
                    <td className="py-2 pr-3">{e.amount}</td>
                    <td className="py-2 pr-3">{e.outstanding}</td>
                  </tr>
                );
              })}
              {entries.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={5}>
                    No AP entries.
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
