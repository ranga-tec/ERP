import { backendFetchJson } from "@/lib/backend.server";
import { Card, SecondaryLink, Table } from "@/components/ui";
import { ItemCreateForm } from "./ItemCreateForm";

type BrandDto = { id: string; code: string; name: string; isActive: boolean };

type ItemDto = {
  id: string;
  sku: string;
  name: string;
  type: number;
  trackingType: number;
  unitOfMeasure: string;
  brandId?: string | null;
  barcode?: string | null;
  defaultUnitCost: number;
  isActive: boolean;
};

const itemTypeLabel: Record<number, string> = {
  1: "Equipment",
  2: "Spare Part",
  3: "Service",
};

const trackingLabel: Record<number, string> = {
  0: "None",
  1: "Serial",
  2: "Batch",
};

export default async function ItemsPage() {
  const [items, brands] = await Promise.all([
    backendFetchJson<ItemDto[]>("/items"),
    backendFetchJson<BrandDto[]>("/brands"),
  ]);

  const brandById = new Map(brands.map((b) => [b.id, b]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Items</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Items, equipment, and services with serial/batch tracking.
        </p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <ItemCreateForm brands={brands.map((b) => ({ id: b.id, code: b.code, name: b.name }))} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">SKU</th>
                <th className="py-2 pr-3">Name</th>
                <th className="py-2 pr-3">Type</th>
                <th className="py-2 pr-3">Tracking</th>
                <th className="py-2 pr-3">UoM</th>
                <th className="py-2 pr-3">Brand</th>
                <th className="py-2 pr-3">Barcode</th>
                <th className="py-2 pr-3">Default Cost</th>
                <th className="py-2 pr-3">Active</th>
                <th className="py-2 pr-3">Label</th>
              </tr>
            </thead>
            <tbody>
              {items.map((i) => (
                <tr key={i.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">{i.sku}</td>
                  <td className="py-2 pr-3">{i.name}</td>
                  <td className="py-2 pr-3">{itemTypeLabel[i.type] ?? i.type}</td>
                  <td className="py-2 pr-3">{trackingLabel[i.trackingType] ?? i.trackingType}</td>
                  <td className="py-2 pr-3">{i.unitOfMeasure}</td>
                  <td className="py-2 pr-3 text-zinc-500">
                    {i.brandId ? brandById.get(i.brandId)?.code ?? "—" : "—"}
                  </td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-500">{i.barcode ?? "—"}</td>
                  <td className="py-2 pr-3">{i.defaultUnitCost}</td>
                  <td className="py-2 pr-3">{i.isActive ? "Yes" : "No"}</td>
                  <td className="py-2 pr-3">
                    <SecondaryLink
                      href={`/api/backend/items/${i.id}/label/pdf`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="px-2 py-1 text-xs"
                    >
                      PDF
                    </SecondaryLink>
                  </td>
                </tr>
              ))}
              {items.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={10}>
                    No items yet.
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
