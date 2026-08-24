export type ReturnDto = { id: string; number: string; pettyCashFundId: string; pettyCashFundCode?: string | null; preparedByName: string; preparedAt: string; notes?: string | null; status: number; submittedAt?: string | null; receivedAt?: string | null; receiptReference?: string | null; rejectionReason?: string | null; cancelledAt?: string | null; totalAmount: number; isLegacyCategoryReturn: boolean; legacyCategoryLineCount: number };
export const RETURN_DRAFT = 0;
export const RETURN_SUBMITTED = 1;
export const returnStatusLabel: Record<number, string> = { 0: "Draft", 1: "Submitted", 2: "Received", 3: "Rejected", 4: "Cancelled" };
export const money = (value: number) => value.toFixed(2);
