"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { ItemLookupField } from "@/components/ItemLookupField";
import { Button, DecimalInput, Input, Select } from "@/components/ui";

type ItemRef = { id: string; sku: string; name: string; unitOfMeasure: string };
type ExpenseAccountRef = { id: string; code: string; name: string };

export function ServiceExpenseClaimLineAddForm({
  claimId,
  items,
  expenseAccounts,
}: {
  claimId: string;
  items: ItemRef[];
  expenseAccounts: ExpenseAccountRef[];
}) {
  const router = useRouter();
  const [itemId, setItemId] = useState("");
  const [expenseAccountId, setExpenseAccountId] = useState("");
  const [description, setDescription] = useState("");
  const [quantity, setQuantity] = useState("1");
  const [unitCost, setUnitCost] = useState("");
  const [billableToCustomer, setBillableToCustomer] = useState(true);
  const [receiptReference, setReceiptReference] = useState("");
  const [missingReceipt, setMissingReceipt] = useState(false);
  const [missingReceiptReason, setMissingReceiptReason] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const parsedQuantity = Number(quantity);
      if (!Number.isFinite(parsedQuantity) || parsedQuantity <= 0) {
        throw new Error("Quantity must be positive.");
      }

      const parsedUnitCost = Number(unitCost);
      if (!Number.isFinite(parsedUnitCost) || parsedUnitCost < 0) {
        throw new Error("Unit cost must be 0 or greater.");
      }

      await apiPostNoContent(`service/expense-claims/${claimId}/lines`, {
        itemId: itemId || null,
        expenseAccountId,
        description: description.trim(),
        quantity: parsedQuantity,
        unitCost: parsedUnitCost,
        billableToCustomer,
        receiptReference: missingReceipt ? null : receiptReference.trim(),
        missingReceipt,
        missingReceiptReason: missingReceipt ? missingReceiptReason.trim() : null,
      });

      setItemId("");
      setExpenseAccountId("");
      setDescription("");
      setQuantity("1");
      setUnitCost("");
      setBillableToCustomer(true);
      setReceiptReference("");
      setMissingReceipt(false);
      setMissingReceiptReason("");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Item (optional)</label>
          <ItemLookupField
            items={items}
            value={itemId}
            onChange={setItemId}
            emptyLabel="Ad-hoc / outside buy"
            searchPlaceholder="Search item by SKU or name..."
          />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Description</label>
          <Input
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            placeholder="Fuse, courier charge, outside machining, wiring, etc."
            required
          />
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Expense category/account *</label>
        <Select value={expenseAccountId} onChange={(event) => setExpenseAccountId(event.target.value)} required>
          <option value="">Select expense account...</option>
          {expenseAccounts.map((account) => <option key={account.id} value={account.id}>{account.code} - {account.name}</option>)}
        </Select>
      </div>

      <div className="grid gap-3 sm:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">
            Quantity{items.find((item) => item.id === itemId)?.unitOfMeasure ? <span className="ml-1 text-xs font-normal text-zinc-500">({items.find((item) => item.id === itemId)?.unitOfMeasure})</span> : null}
          </label>
          <DecimalInput value={quantity} onChange={(event) => setQuantity(event.target.value)} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Unit cost</label>
          <DecimalInput value={unitCost} onChange={(event) => setUnitCost(event.target.value)} required />
        </div>
        <label className="flex items-center gap-2 rounded-xl border border-[var(--input-border)] bg-[var(--surface)] px-3 py-2 text-sm shadow-[var(--shadow-control)]">
          <input
            type="checkbox"
            checked={billableToCustomer}
            onChange={(event) => setBillableToCustomer(event.target.checked)}
            className="h-4 w-4 rounded border-zinc-300"
          />
          Billable to customer
        </label>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Receipt / bill number {missingReceipt ? "(exception requested)" : "*"}</label>
          <Input value={receiptReference} onChange={(event) => setReceiptReference(event.target.value)} required={!missingReceipt} disabled={missingReceipt} />
        </div>
        <label className="flex items-center gap-2 rounded-xl border border-[var(--input-border)] px-3 py-2 text-sm">
          <input type="checkbox" checked={missingReceipt} onChange={(event) => setMissingReceipt(event.target.checked)} />
          Missing receipt — request special approval
        </label>
      </div>
      {missingReceipt ? <div><label className="mb-1 block text-sm font-medium">Missing receipt reason *</label><Input value={missingReceiptReason} onChange={(event) => setMissingReceiptReason(event.target.value)} required /></div> : null}
      <p className="text-xs text-zinc-500">After adding the line, upload its receipt file from the line-evidence panel before submission.</p>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Adding..." : "Add Expense Line"}
      </Button>
    </form>
  );
}
