export type LedgerAccountDto = {
  id: string;
  code: string;
  name: string;
  accountType: number;
  parentAccountId?: string | null;
  parentAccountCode?: string | null;
  parentAccountName?: string | null;
  allowsPosting: boolean;
  description?: string | null;
  isActive: boolean;
};

export const ledgerAccountTypeOptions = [
  { value: "1", label: "Asset" },
  { value: "2", label: "Liability" },
  { value: "3", label: "Equity" },
  { value: "4", label: "Revenue" },
  { value: "5", label: "Expense" },
] as const;

const typeLabels = new Map(ledgerAccountTypeOptions.map((option) => [Number(option.value), option.label]));

export function ledgerAccountTypeLabel(accountType: number): string {
  return typeLabels.get(accountType) ?? String(accountType);
}
