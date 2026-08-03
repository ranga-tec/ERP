export const RETURN_DRAFT = 0;
export const RETURN_SUBMITTED = 1;
export const RETURN_RECEIVED = 2;
export const RETURN_REJECTED = 3;
export const RETURN_CANCELLED = 4;

export const returnStatusLabel: Record<number, string> = {
  [RETURN_DRAFT]: "Draft",
  [RETURN_SUBMITTED]: "Awaiting head office",
  [RETURN_RECEIVED]: "Received",
  [RETURN_REJECTED]: "Rejected",
  [RETURN_CANCELLED]: "Cancelled",
};

export const categoryLabel: Record<number, string> = {
  1: "Job Wise",
  2: "Emergency Operation",
  3: "Transportation",
  4: "Custom",
};

export function describeReturnCategory(category: number, jobNumber?: string | null, customName?: string | null) {
  if (category === 1 && jobNumber) return `Job Wise - ${jobNumber}`;
  if (category === 4 && customName) return customName;
  return categoryLabel[category] ?? `Category ${category}`;
}

export function money(value: number) {
  return value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export type ReturnCandidateDto = {
  pettyCashRequestLineId: string;
  pettyCashFundId: string;
  pettyCashFundCode: string;
  requestNumber: string;
  category: number;
  serviceJobNumber?: string | null;
  customCategoryName?: string | null;
  purpose: string;
  fundedAmount: number;
  ledgerBalance: number;
  pendingReturnAmount: number;
  pendingReallocationAmount: number;
  availableToReturn: number;
  openIouCount: number;
};
