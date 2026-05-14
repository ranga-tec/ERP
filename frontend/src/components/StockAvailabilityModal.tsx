"use client";

import { useState } from "react";
import { Button, SecondaryButton } from "@/components/ui";
import { StockAvailabilityExplorer } from "@/components/StockAvailabilityExplorer";

type WarehouseRef = { id: string; code: string; name: string };
type ItemRef = { id: string; sku: string; name: string };

export function StockAvailabilityModal({
  warehouses,
  items,
  initialWarehouseId,
}: {
  warehouses: WarehouseRef[];
  items: ItemRef[];
  initialWarehouseId?: string;
}) {
  const [open, setOpen] = useState(false);

  return (
    <>
      <Button type="button" onClick={() => setOpen(true)}>
        Load stock
      </Button>

      {open ? (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-zinc-950/55 p-4">
          <div
            role="dialog"
            aria-modal="true"
            aria-label="Stock visibility"
            className="max-h-[88vh] w-full max-w-5xl overflow-auto rounded-lg border border-[var(--card-border)] bg-[var(--card-bg)] p-4 shadow-xl"
          >
            <div className="mb-3 flex items-center justify-between gap-3">
              <div className="text-sm font-semibold">Stock visibility</div>
              <SecondaryButton type="button" onClick={() => setOpen(false)}>
                Close
              </SecondaryButton>
            </div>
            <StockAvailabilityExplorer warehouses={warehouses} items={items} initialWarehouseId={initialWarehouseId} />
          </div>
        </div>
      ) : null}
    </>
  );
}
