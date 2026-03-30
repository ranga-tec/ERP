import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card } from "@/components/ui";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";
import { ServiceContractEditForm } from "../ServiceContractEditForm";

type CustomerDto = { id: string; code: string; name: string };
type EquipmentUnitDto = { id: string; serialNumber: string; customerId: string };
type ServiceContractDto = {
  id: string;
  number: string;
  customerId: string;
  equipmentUnitId: string;
  contractType: number;
  coverage: number;
  startDate: string;
  endDate: string;
  notes?: string | null;
  isActive: boolean;
  currentState: string;
};

const contractTypeLabel: Record<number, string> = {
  0: "Annual Maintenance",
  1: "Service Level Agreement",
  2: "Warranty Extension",
};

const coverageLabel: Record<number, string> = {
  0: "No Warranty",
  1: "Inspection Only",
  2: "Labor Only",
  3: "Parts Only",
  4: "Labor and Parts",
};

export default async function ServiceContractDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [contract, customers, units] = await Promise.all([
    backendFetchJson<ServiceContractDto>(`/service/contracts/${id}`),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<EquipmentUnitDto[]>("/service/equipment-units?take=500"),
  ]);

  const customerById = new Map(customers.map((customer) => [customer.id, customer]));
  const unitById = new Map(units.map((unit) => [unit.id, unit]));

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/service/contracts" className="hover:underline">
            Service Contracts
          </Link>{" "}
          / <span className="font-mono text-xs">{contract.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Contract {contract.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>Customer: {customerById.get(contract.customerId)?.code ?? contract.customerId}</div>
          <div>Equipment: {unitById.get(contract.equipmentUnitId)?.serialNumber ?? contract.equipmentUnitId}</div>
          <div>Type: {contractTypeLabel[contract.contractType] ?? contract.contractType}</div>
          <div>Coverage: {coverageLabel[contract.coverage] ?? contract.coverage}</div>
          <div>State: {contract.currentState}</div>
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-3">
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Coverage Window</div>
          <div className="mt-2 text-sm font-medium">
            {new Date(contract.startDate).toLocaleDateString()} - {new Date(contract.endDate).toLocaleDateString()}
          </div>
        </Card>
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Contract Type</div>
          <div className="mt-2 text-sm font-medium">{contractTypeLabel[contract.contractType] ?? contract.contractType}</div>
        </Card>
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Billing Impact</div>
          <div className="mt-2 text-sm font-medium">
            {contract.coverage === 4 ? "Can fully cover customer billing" : "May require mixed covered/billable handling"}
          </div>
        </Card>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Contract Details</div>
        <ServiceContractEditForm contract={contract} customers={customers} equipmentUnits={units} />
      </Card>

      {contract.notes ? (
        <Card>
          <div className="mb-2 text-sm font-semibold">Notes</div>
          <div className="whitespace-pre-wrap text-sm text-zinc-700 dark:text-zinc-200">{contract.notes}</div>
        </Card>
      ) : null}

      <DocumentCollaborationPanel referenceType="SC" referenceId={id} />
    </div>
  );
}
