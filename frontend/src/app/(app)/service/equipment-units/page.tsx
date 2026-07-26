import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { AuditStamp } from "@/components/AuditStamp";
import { ItemInlineLink } from "@/components/InlineLink";
import { SearchableRow, SearchableTable } from "@/components/SearchableTable";
import { Card } from "@/components/ui";
import { EquipmentUnitCreateForm } from "./EquipmentUnitCreateForm";
import { EquipmentUnitEditForm } from "./EquipmentUnitEditForm";

type EquipmentUnitDto = {
  id: string;
  itemId: string;
  serialNumber: string;
  customerId: string;
  purchasedAt?: string | null;
  warrantyUntil?: string | null;
  warrantyCoverage: number;
  serviceIntervalDays?: number | null;
  nextServiceDueAt?: string | null;
  nextRepairDueAt?: string | null;
  hasActiveWarranty: boolean;
  isActive: boolean;
  createdAt: string;
  createdByName?: string | null;
  lastModifiedAt?: string | null;
  lastModifiedByName?: string | null;
};

type ItemDto = { id: string; sku: string; name: string; type: number };
type CustomerDto = { id: string; code: string; name: string };

export default async function EquipmentUnitsPage() {
  const [units, items, customers] = await Promise.all([
    backendFetchJson<EquipmentUnitDto[]>("/service/equipment-units?take=100&includeInactive=true"),
    backendFetchJson<ItemDto[]>("/items/options"),
    backendFetchJson<CustomerDto[]>("/customers"),
  ]);

  const itemById = new Map(items.map((item) => [item.id, item]));
  const customerById = new Map(customers.map((customer) => [customer.id, customer]));

  const equipmentItems = items.filter((item) => item.type === 1).map((item) => ({ id: item.id, sku: item.sku, name: item.name }));
  const coverageLabel: Record<number, string> = {
    0: "No Warranty",
    1: "Inspection Only",
    2: "Labor Only",
    3: "Parts Only",
    4: "Labor and Parts",
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Equipment Units</h1>
          <p className="mt-1 text-sm text-zinc-500">Track sold and outside equipment by serial number for job orders.</p>
        </div>
        <AppFormModal title="Create Equipment Unit" description="Register a customer asset with warranty, service interval, and next due dates." buttonLabel="+ New Equipment Unit" size="xl">
          <EquipmentUnitCreateForm equipmentItems={equipmentItems} customers={customers} />
        </AppFormModal>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <SearchableTable
          placeholder="Search serial, item, customer, warranty..."
          emptyMessage="No equipment units yet."
          emptyColSpan={11}
          filter={{
            label: "Filter by status",
            defaultValue: "active",
            options: [
              { value: "active", label: "Active only" },
              { value: "inactive", label: "Inactive only" },
              { value: "all", label: "All statuses" },
            ],
          }}
          headers={
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Serial</th>
                <th className="py-2 pr-3">Item</th>
                <th className="py-2 pr-3">Customer</th>
                <th className="py-2 pr-3">Purchased</th>
                <th className="py-2 pr-3">Warranty</th>
                <th className="py-2 pr-3">Coverage</th>
                <th className="py-2 pr-3">In Warranty</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Created</th>
                <th className="py-2 pr-3">Modified</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
          }
        >
              {units.map((unit) => {
                const item = itemById.get(unit.itemId);
                const customer = customerById.get(unit.customerId);
                const coverage = coverageLabel[unit.warrantyCoverage] ?? String(unit.warrantyCoverage);
                return (
                <SearchableRow
                  key={unit.id}
                  filterKey={unit.isActive ? "active" : "inactive"}
                  searchText={[
                    unit.serialNumber,
                    item?.sku,
                    item?.name,
                    customer?.code,
                    customer?.name,
                    coverage,
                    unit.hasActiveWarranty ? "active warranty yes" : "inactive warranty no",
                    unit.isActive ? "status active" : "status inactive retired",
                  ].filter(Boolean).join(" ")}
                >
                <tr key={unit.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/service/equipment-units/${unit.id}`}>
                      {unit.serialNumber}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">
                    <ItemInlineLink itemId={unit.itemId}>
                      {itemById.get(unit.itemId)?.sku ?? unit.itemId}
                    </ItemInlineLink>
                  </td>
                  <td className="py-2 pr-3">{customerById.get(unit.customerId)?.code ?? unit.customerId}</td>
                  <td className="py-2 pr-3 text-zinc-500">
                    {unit.purchasedAt ? new Date(unit.purchasedAt).toLocaleDateString() : "-"}
                  </td>
                  <td className="py-2 pr-3 text-zinc-500">
                    {unit.warrantyUntil ? new Date(unit.warrantyUntil).toLocaleDateString() : "-"}
                  </td>
                  <td className="py-2 pr-3 text-zinc-500">{coverageLabel[unit.warrantyCoverage] ?? unit.warrantyCoverage}</td>
                  <td className="py-2 pr-3">{unit.hasActiveWarranty ? "Yes" : "No"}</td>
                  <td className="py-2 pr-3">
                    {unit.isActive ? (
                      <span className="text-zinc-500">Active</span>
                    ) : (
                      <span className="rounded bg-zinc-200 px-1.5 py-0.5 text-[11px] font-medium text-zinc-700 dark:bg-zinc-700 dark:text-zinc-200">
                        Inactive
                      </span>
                    )}
                  </td>
                  <td className="py-2 pr-3 align-top text-xs">
                    <AuditStamp at={unit.createdAt} by={unit.createdByName} />
                  </td>
                  <td className="py-2 pr-3 align-top text-xs">
                    <AuditStamp at={unit.lastModifiedAt} by={unit.lastModifiedByName} />
                  </td>
                  <td className="py-2 pr-3">
                    <div className="flex flex-wrap items-center gap-3 text-xs">
                      <Link className="font-semibold text-[var(--link)] underline underline-offset-2" href={`/service/equipment-units/${unit.id}`}>
                        View
                      </Link>
                      <AppFormModal title={`Edit Equipment Unit ${unit.serialNumber}`} description="Update customer, warranty, service interval, and next due dates." buttonLabel="Edit" variant="secondary">
                        <EquipmentUnitEditForm unit={unit} customers={customers} />
                      </AppFormModal>
                    </div>
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
