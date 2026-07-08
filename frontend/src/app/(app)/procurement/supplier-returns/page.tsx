import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { ListViewEditActions } from "@/components/ListViewEditActions";
import { SearchableRow, SearchableTable } from "@/components/SearchableTable";
import { Card } from "@/components/ui";
import { SupplierReturnCreateForm } from "./SupplierReturnCreateForm";

type SupplierReturnSummaryDto = {
  id: string;
  number: string;
  supplierId: string;
  warehouseId: string;
  returnDate: string;
  status: number;
  reason?: string | null;
};

type SupplierDto = { id: string; code: string; name: string };
type WarehouseDto = { id: string; code: string; name: string };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Voided",
};

export default async function SupplierReturnsPage() {
  const [returns, suppliers, warehouses] = await Promise.all([
    backendFetchJson<SupplierReturnSummaryDto[]>("/procurement/supplier-returns?take=100"),
    backendFetchJson<SupplierDto[]>("/suppliers"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
  ]);

  const supplierById = new Map(suppliers.map((s) => [s.id, s]));
  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Supplier Returns</h1>
        <p className="mt-1 text-sm text-zinc-500">Return inventory back to a supplier, then post.</p>
      </div>

      <AppFormModal title="Create Supplier Return" description="Create a draft return to supplier." buttonLabel="+ New Supplier Return">
        <SupplierReturnCreateForm suppliers={suppliers} warehouses={warehouses} />
      </AppFormModal>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <SearchableTable placeholder="Search supplier returns..." emptyMessage="No supplier returns yet." emptyColSpan={7} headers={
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Supplier</th>
                <th className="py-2 pr-3">Warehouse</th>
                <th className="py-2 pr-3">Return Date</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Reason</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
          }>
              {returns.map((r) => {
                const supplier = supplierById.get(r.supplierId)?.code ?? r.supplierId;
                const warehouse = warehouseById.get(r.warehouseId)?.code ?? r.warehouseId;
                const status = statusLabel[r.status] ?? String(r.status);
                return (
                <SearchableRow key={r.id} searchText={[r.number, supplier, warehouse, status, r.reason ?? ""].join(" ")}>
                <tr key={r.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/procurement/supplier-returns/${r.id}`}>
                      {r.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">{supplier}</td>
                  <td className="py-2 pr-3">{warehouse}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(r.returnDate).toLocaleString()}</td>
                  <td className="py-2 pr-3">{status}</td>
                  <td className="py-2 pr-3 text-zinc-500">{r.reason ?? "-"}</td>
                  <td className="py-2 pr-3">
                    <ListViewEditActions
                      viewHref={`/procurement/supplier-returns/${r.id}`}
                      canEdit={r.status === 0}
                      auditTableName="SupplierReturns"
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
