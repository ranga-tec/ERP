import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { ListViewEditActions } from "@/components/ListViewEditActions";
import { SearchableRow, SearchableTable } from "@/components/SearchableTable";
import { Card } from "@/components/ui";
import { StockAdjustmentCreateForm } from "./StockAdjustmentCreateForm";

type StockAdjustmentSummaryDto = {
  id: string;
  number: string;
  warehouseId: string;
  adjustedAt: string;
  status: number;
};

type WarehouseDto = { id: string; code: string; name: string };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Voided",
};

export default async function StockAdjustmentsPage() {
  const [adjustments, warehouses] = await Promise.all([
    backendFetchJson<StockAdjustmentSummaryDto[]>("/inventory/stock-adjustments?take=100"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
  ]);

  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Stock Adjustments</h1>
          <p className="mt-1 text-sm text-zinc-500">Adjust inventory up/down (counts, damage, corrections).</p>
        </div>
        <AppFormModal title="Create Stock Adjustment" description="Create a draft adjustment before adding adjustment lines." buttonLabel="+ New Adjustment">
          <StockAdjustmentCreateForm warehouses={warehouses} />
        </AppFormModal>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <SearchableTable
          placeholder="Search stock adjustments..."
          emptyMessage="No stock adjustments yet."
          emptyColSpan={5}
          headers={
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Warehouse</th>
                <th className="py-2 pr-3">Adjusted</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
          }
        >
          {adjustments.map((a) => (
            <SearchableRow
              key={a.id}
              searchText={`${a.number} ${warehouseById.get(a.warehouseId)?.code ?? a.warehouseId} ${statusLabel[a.status] ?? a.status}`}
            >
                <tr key={a.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/inventory/stock-adjustments/${a.id}`}>
                      {a.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">{warehouseById.get(a.warehouseId)?.code ?? a.warehouseId}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(a.adjustedAt).toLocaleString()}</td>
                  <td className="py-2 pr-3">{statusLabel[a.status] ?? a.status}</td>
                  <td className="py-2 pr-3">
                    <ListViewEditActions
                      viewHref={`/inventory/stock-adjustments/${a.id}`}
                      canEdit={a.status === 0}
                      editInModal
                      editModalTitle={`Edit Stock Adjustment ${a.number}`}
                    />
                  </td>
                </tr>
            </SearchableRow>
          ))}
        </SearchableTable>
      </Card>
    </div>
  );
}
