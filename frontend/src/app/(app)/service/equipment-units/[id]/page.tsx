import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { ItemInlineLink } from "@/components/InlineLink";
import { Card } from "@/components/ui";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";
import { EquipmentUnitEditForm } from "../EquipmentUnitEditForm";

type EquipmentUnitDto = {
  id: string;
  itemId: string;
  serialNumber: string;
  customerId: string;
  purchasedAt?: string | null;
  warrantyUntil?: string | null;
  warrantyCoverage: number;
  hasActiveWarranty: boolean;
};

type ItemDto = { id: string; sku: string; name: string };
type CustomerDto = { id: string; code: string; name: string };
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

export default async function EquipmentUnitDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [unit, items, customers, contracts] = await Promise.all([
    backendFetchJson<EquipmentUnitDto>(`/service/equipment-units/${id}`),
    backendFetchJson<ItemDto[]>("/items/options"),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<ServiceContractSummaryDto[]>(`/service/contracts?equipmentUnitId=${id}`),
  ]);

  const itemById = new Map(items.map((item) => [item.id, item]));
  const customerById = new Map(customers.map((customer) => [customer.id, customer]));
  const coverageLabel: Record<number, string> = {
    0: "No Warranty",
    1: "Inspection Only",
    2: "Labor Only",
    3: "Parts Only",
    4: "Labor and Parts",
  };
  const contractTypeLabel: Record<number, string> = {
    0: "AMC",
    1: "SLA",
    2: "Warranty Extension",
  };

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/service/equipment-units" className="hover:underline">
            Equipment Units
          </Link>{" "}
          / <span className="font-mono text-xs">{unit.serialNumber}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Unit {unit.serialNumber}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>
            Item:{" "}
            <ItemInlineLink itemId={unit.itemId}>
              {itemById.get(unit.itemId)?.sku ?? unit.itemId}
            </ItemInlineLink>
          </div>
          <div>Customer: {customerById.get(unit.customerId)?.code ?? unit.customerId}</div>
          <div>Purchased: {unit.purchasedAt ? new Date(unit.purchasedAt).toLocaleDateString() : "-"}</div>
          <div>Warranty: {unit.warrantyUntil ? new Date(unit.warrantyUntil).toLocaleDateString() : "-"}</div>
          <div>Coverage: {coverageLabel[unit.warrantyCoverage] ?? unit.warrantyCoverage}</div>
          <div>Active Warranty: {unit.hasActiveWarranty ? "Yes" : "No"}</div>
        </div>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Unit Details</div>
        <EquipmentUnitEditForm unit={unit} customers={customers} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Linked Service Contracts</div>
        {contracts.length > 0 ? (
          <div className="space-y-3 text-sm">
            {contracts.map((contract) => (
              <div key={contract.id} className="rounded-xl border border-[var(--card-border)] p-3">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <Link className="font-mono text-xs hover:underline" href={`/service/contracts/${contract.id}`}>
                    {contract.number}
                  </Link>
                  <span className="text-xs text-zinc-500">{contract.currentState}</span>
                </div>
                <div className="mt-2 text-zinc-500">
                  {contractTypeLabel[contract.contractType] ?? contract.contractType} - {coverageLabel[contract.coverage] ?? contract.coverage}
                </div>
                <div className="mt-1 text-zinc-500">
                  {new Date(contract.startDate).toLocaleDateString()} - {new Date(contract.endDate).toLocaleDateString()}
                </div>
              </div>
            ))}
          </div>
        ) : (
          <div className="text-sm text-zinc-500">
            No service contracts are linked to this unit yet. Create one from{" "}
            <Link className="underline" href="/service/contracts">
              Service Contracts
            </Link>
            .
          </div>
        )}
      </Card>

      <DocumentCollaborationPanel referenceType="EUNIT" referenceId={id} />
    </div>
  );
}
