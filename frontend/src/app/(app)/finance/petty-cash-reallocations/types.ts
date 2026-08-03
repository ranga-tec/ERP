export const REALLOCATION_DRAFT = 0;
export const REALLOCATION_SUBMITTED = 1;
export const REALLOCATION_APPROVED = 2;
export const REALLOCATION_REJECTED = 3;
export const REALLOCATION_CANCELLED = 4;

export const reallocationStatusLabel: Record<number, string> = {
  [REALLOCATION_DRAFT]: "Draft",
  [REALLOCATION_SUBMITTED]: "Awaiting head office",
  [REALLOCATION_APPROVED]: "Approved and posted",
  [REALLOCATION_REJECTED]: "Rejected",
  [REALLOCATION_CANCELLED]: "Cancelled",
};

const categoryLabel: Record<number, string> = {
  1: "Job Wise",
  2: "Emergency Operation",
  3: "Transportation",
  4: "Custom",
};

export function describeCategory(category: number, jobNumber?: string | null, customName?: string | null) {
  if (category === 1 && jobNumber) return `Job Wise - ${jobNumber}`;
  if (category === 4 && customName) return customName;
  return categoryLabel[category] ?? `Category ${category}`;
}

export function money(value: number) {
  return value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export type CategoryReferenceDto = {
  pettyCashRequestLineId: string;
  pettyCashRequestId: string;
  requestNumber: string;
  category: number;
  serviceJobNumber?: string | null;
  customCategoryName?: string | null;
  purpose: string;
};

export type ReallocationCandidateDto = CategoryReferenceDto & {
  pettyCashFundId: string;
  pettyCashFundCode: string;
  fundedAmount: number;
  ledgerBalance: number;
  pendingReturnAmount: number;
  pendingReallocationAmount: number;
  availableBalance: number;
};

export type ReallocationDto = {
  id: string;
  number: string;
  pettyCashFundId: string;
  pettyCashFundCode?: string | null;
  requestedByName: string;
  requestedAt: string;
  reason: string;
  status: number;
  submittedAt?: string | null;
  approvedAt?: string | null;
  rejectedAt?: string | null;
  rejectionReason?: string | null;
  cancelledAt?: string | null;
  amount: number;
  sourceBalance: number;
  destinationBalance: number;
  source: CategoryReferenceDto;
  destination: CategoryReferenceDto;
};
