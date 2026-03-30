import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { ServiceContractCreateForm } from "./ServiceContractCreateForm";

type CustomerDto = { id: string; code: string; name: string };
type EquipmentUnitDto = { id: string; serialNumber: string; customerId: string };
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
  const [contracts, customers, units] = await Promise.all([
    backendFetchJson<ServiceContractSummaryDto[]>("/service/contracts"),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<EquipmentUnitDto[]>("/service/equipment-units?take=500"),
  ]);

  const customerById = new Map(customers.map((customer) => [customer.id, customer]));
  const unitById = new Map(units.map((unit) => [unit.id, unit]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Service Contracts</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Track asset-level AMC, SLA, and warranty-extension coverage that can drive service-job entitlement.
        </p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create Contract</div>
        <ServiceContractCreateForm customers={customers} equipmentUnits={units} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Contracts</div>
        <div className="overflow-auto">
          <Table>
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
            <tbody>
              {contracts.map((contract) => (
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
              ))}
              {contracts.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={8}>
                    No service contracts yet.
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
