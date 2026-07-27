"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPut } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

/**
 * Whole-invoice discount, entered as a percentage or a flat amount. Only one applies, so the
 * form keeps a single value and a kind rather than two fields that can disagree.
 */
export function InvoiceDiscountForm({
  invoiceId,
  discountPercent,
  discountAmount,
}: {
  invoiceId: string;
  discountPercent: number;
  discountAmount: number;
}) {
  const router = useRouter();
  const [kind, setKind] = useState<"percent" | "amount">(discountAmount > 0 ? "amount" : "percent");
  const [value, setValue] = useState(String(discountAmount > 0 ? discountAmount : discountPercent));
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function save() {
    setError(null);
    setBusy(true);
    try {
      const parsed = Number(value);
      if (!Number.isFinite(parsed) || parsed < 0) {
        throw new Error("Discount must be 0 or greater.");
      }

      await apiPut(`sales/invoices/${invoiceId}/discount`, {
        discountPercent: kind === "percent" ? parsed : 0,
        discountAmount: kind === "amount" ? parsed : 0,
      });
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-end gap-2">
        <Select
          className="w-28"
          value={kind}
          onChange={(e) => setKind(e.target.value as "percent" | "amount")}
          disabled={busy}
        >
          <option value="percent">%</option>
          <option value="amount">Amount</option>
        </Select>
        <Input className="w-40" value={value} onChange={(e) => setValue(e.target.value)} inputMode="decimal" disabled={busy} />
        <Button type="button" onClick={save} disabled={busy}>
          {busy ? "Saving..." : "Apply discount"}
        </Button>
      </div>
      {error ? <div className="text-xs text-red-700 dark:text-red-300">{error}</div> : null}
    </div>
  );
}
