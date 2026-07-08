import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { SearchableRow, SearchableTable } from "@/components/SearchableTable";
import { Card } from "@/components/ui";
import { ServiceContractCreateForm } from "./ServiceContractCreateForm";

type CustomerDto = { id: string; code: string; name: string };
type EquipmentUnitDto = { id: string; serialNumber: string; itemId: string; customerId: string };
type ItemDto = { id: string; sku: string; name: string };
type ServiceContractSummaryDto = {
  id: string;
  number: string;
  customerId: string;
  equipmentUnitId: string;
  contractType: number;
  coverage: number;
  startDate: string;
  endDate: string;
  isActive: boolean;
  currentState: string;
};

const contractTypeLabel: Record<number, string> = {
  0: "AMC",
  1: "SLA",
  2: "Warranty Extension",
};

const coverageLabel: Record<number, string> = {
  0: "No Warranty",
  1: "Inspection Only",
  2: "Labor Only",
  3: "Parts Only",
  4: "Labor and Parts",
};

export default async function ServiceContractsPage() {
  const [contracts, customers, units, items] = await Promise.all([
    backendFetchJson<ServiceContractSummaryDto[]>("/service/contracts"),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<EquipmentUnitDto[]>("/service/equipment-units?take=2000"),
    backendFetchJson<ItemDto[]>("/items/options"),
  ]);

  const customerById = new Map(customers.map((customer) => [customer.id, customer]));
  const unitById = new Map(units.map((unit) => [unit.id, unit]));
  const itemById = new Map(items.map((item) => [item.id, item]));
  const equipmentUnitOptions = units.map((unit) => {
    const item = itemById.get(unit.itemId);
    const customer = customerById.get(unit.customerId);
    return {
      ...unit,
      itemSku: item?.sku,
      itemName: item?.name,
      customerCode: customer?.code,
    };
  });

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Service Contracts</h1>
          <p className="mt-1 text-sm text-zinc-500">
            Track asset-level AMC, SLA, and warranty-extension coverage that can drive service-job entitlement.
          </p>
        </div>
        <AppFormModal title="Create Service Contract" description="Create asset-level service coverage for a customer equipment unit." buttonLabel="+ New Contract" size="xl">
          <ServiceContractCreateForm customers={customers} equipmentUnits={equipmentUnitOptions} />
        </AppFormModal>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Contracts</div>
        <SearchableTable
          placeholder="Search contract, equipment, customer, type, coverage..."
          emptyMessage="No service contracts yet."
          emptyColSpan={8}
          headers={
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Equipment</th>
                <th className="py-2 pr-3">Customer</th>
                <th className="py-2 pr-3">Type</th>
                <th className="py-2 pr-3">Coverage</th>
                <th className="py-2 pr-3">Dates</th>
                <th className="py-2 pr-3">State</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
          }
        >
              {contracts.map((contract) => {
                const unit = unitById.get(contract.equipmentUnitId);
                const customer = customerById.get(contract.customerId);
                const type = contractTypeLabel[contract.contractType] ?? String(contract.contractType);
                const coverage = coverageLabel[contract.coverage] ?? String(contract.coverage);
                return (
                <SearchableRow
                  key={contract.id}
                  searchText={[
                    contract.number,
                    unit?.serialNumber,
                    customer?.code,
                    customer?.name,
                    type,
                    coverage,
                    contract.currentState,
                    contract.isActive ? "active" : "inactive",
                  ].filter(Boolean).join(" ")}
                >
                <tr key={contract.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/service/contracts/${contract.id}`}>
                      {contract.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-600 dark:text-zinc-400">
                    {unitById.get(contract.equipmentUnitId)?.serialNumber ?? contract.equipmentUnitId}
                  </td>
                  <td className="py-2 pr-3">{customerById.get(contract.customerId)?.code ?? contract.customerId}</td>
                  <td className="py-2 pr-3">{contractTypeLabel[contract.contractType] ?? contract.contractType}</td>
                  <td className="py-2 pr-3">{coverageLabel[contract.coverage] ?? contract.coverage}</td>
                  <td className="py-2 pr-3 text-zinc-500">
                    {new Date(contract.startDate).toLocaleDateString()} - {new Date(contract.endDate).toLocaleDateString()}
                  </td>
                  <td className="py-2 pr-3">{contract.currentState}</td>
                  <td className="py-2 pr-3">
                    <div className="flex flex-wrap gap-3 text-xs">
                      <Link className="font-semibold text-[var(--link)] underline underline-offset-2" href={`/service/contracts/${contract.id}`}>
                        View
                      </Link>
                      <Link className="font-semibold text-[var(--link)] underline underline-offset-2" href={`/service/contracts/${contract.id}`}>
                        Edit
                      </Link>
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
