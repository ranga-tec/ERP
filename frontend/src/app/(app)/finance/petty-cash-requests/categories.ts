// Mirrors PettyCashRequestCategory and PettyCashRequestStatus on the backend. Kept in one place so
// the list, detail and line forms cannot drift from each other.

export const CATEGORY_JOB_WISE = 1;
export const CATEGORY_EMERGENCY = 2;
export const CATEGORY_TRANSPORT = 3;
export const CATEGORY_CUSTOM = 4;

export const categoryLabel: Record<number, string> = {
  [CATEGORY_JOB_WISE]: "Job Wise",
  [CATEGORY_EMERGENCY]: "Emergency Operation",
  [CATEGORY_TRANSPORT]: "Transportation",
  [CATEGORY_CUSTOM]: "Custom",
};

export const categoryOptions = [
  { value: CATEGORY_JOB_WISE, label: categoryLabel[CATEGORY_JOB_WISE] },
  { value: CATEGORY_EMERGENCY, label: categoryLabel[CATEGORY_EMERGENCY] },
  { value: CATEGORY_TRANSPORT, label: categoryLabel[CATEGORY_TRANSPORT] },
  { value: CATEGORY_CUSTOM, label: categoryLabel[CATEGORY_CUSTOM] },
];

export const STATUS_DRAFT = 0;
export const STATUS_SUBMITTED = 1;
export const STATUS_APPROVED = 2;
export const STATUS_PARTIALLY_FUNDED = 3;
export const STATUS_FUNDED = 4;
export const STATUS_REJECTED = 5;
export const STATUS_CANCELLED = 6;

export const statusLabel: Record<number, string> = {
  [STATUS_DRAFT]: "Draft",
  [STATUS_SUBMITTED]: "Submitted",
  [STATUS_APPROVED]: "Approved",
  [STATUS_PARTIALLY_FUNDED]: "Partially Funded",
  [STATUS_FUNDED]: "Funded",
  [STATUS_REJECTED]: "Rejected",
  [STATUS_CANCELLED]: "Cancelled",
};

/** Describes a line in one phrase: the category, plus the job or custom name that qualifies it. */
export function describeCategory(
  category: number,
  serviceJobNumber?: string | null,
  customCategoryName?: string | null,
): string {
  if (category === CATEGORY_JOB_WISE) {
    return serviceJobNumber ? `Job Wise - ${serviceJobNumber}` : "Job Wise - job removed";
  }

  if (category === CATEGORY_CUSTOM) {
    return customCategoryName ? `Custom - ${customCategoryName}` : "Custom";
  }

  return categoryLabel[category] ?? String(category);
}

export function money(value: number): string {
  return value.toFixed(2);
}
