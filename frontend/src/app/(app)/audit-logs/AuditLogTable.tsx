"use client";

import { useState } from "react";
import { Card, Input, Table } from "@/components/ui";

export type AuditLogDto = {
  id: string;
  occurredAt: string;
  userId?: string | null;
  userLabel?: string | null;
  tableName: string;
  tableLabel: string;
  action: number;
  key: string;
  isTechnical: boolean;
  changesJson: string;
};

type ParsedChange = {
  field: string;
  oldValue: unknown;
  newValue: unknown;
  isSystemField: boolean;
};

const actionLabel: Record<number, string> = {
  1: "Insert",
  2: "Update",
  3: "Delete",
};

const systemFields = new Set(["CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy"]);

const enumMaps: Record<string, Record<string, Record<string, string>>> = {
  DirectPurchases: {
    Status: { "0": "Draft", "1": "Posted", "2": "Voided" },
  },
  InventoryMovements: {
    Type: {
      "1": "Receipt",
      "2": "Issue",
      "3": "Adjustment",
      "4": "Transfer In",
      "5": "Transfer Out",
      "6": "Consumption",
      "7": "Supplier Return",
    },
  },
  Payments: {
    Direction: { "1": "Incoming", "2": "Outgoing" },
    CounterpartyType: { "1": "Customer", "2": "Supplier" },
  },
  PurchaseOrders: {
    Status: { "0": "Draft", "1": "Approved", "2": "Partially Received", "3": "Closed", "4": "Cancelled" },
  },
  PurchaseRequisitions: {
    Status: { "0": "Draft", "1": "Submitted", "2": "Approved", "3": "Rejected", "4": "Cancelled" },
  },
  ServiceEstimates: {
    Status: { "0": "Draft", "1": "Approved", "2": "Rejected" },
    CustomerApprovalStatus: { "0": "Not Sent", "1": "Pending", "2": "Approved", "3": "Rejected" },
  },
  ServiceExpenseClaims: {
    Status: { "0": "Draft", "1": "Submitted", "2": "Approved", "3": "Rejected", "4": "Settled" },
    FundingSource: { "1": "Out of Pocket", "2": "Petty Cash" },
  },
  ServiceHandovers: {
    Status: { "0": "Draft", "1": "Completed", "2": "Cancelled" },
  },
  ServiceJobs: {
    Kind: { "0": "Service", "1": "Repair" },
    Status: { "0": "Open", "1": "In Progress", "2": "Completed", "3": "Closed", "4": "Cancelled" },
    EntitlementSource: { "0": "None", "1": "Manufacturer Warranty", "2": "Service Contract" },
    EntitlementCoverage: { "0": "None", "1": "Inspection Only", "2": "Labor Only", "3": "Parts Only", "4": "Labor and Parts" },
    CustomerBillingTreatment: { "0": "Billable", "1": "Partially Covered", "2": "Covered No Charge" },
  },
  StockAdjustments: {
    Status: { "0": "Draft", "1": "Posted", "2": "Voided" },
  },
  StockTransfers: {
    Status: { "0": "Draft", "1": "Posted", "2": "Voided" },
  },
  WorkOrders: {
    Status: { "0": "Open", "1": "In Progress", "2": "Done", "3": "Cancelled" },
  },
};

function looksLikeIsoDate(value: string): boolean {
  return /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/.test(value);
}

function humanizeField(field: string): string {
  return field.replace(/([a-z0-9])([A-Z])/g, "$1 $2");
}

function parseChanges(changesJson: string): ParsedChange[] {
  try {
    const parsed = JSON.parse(changesJson) as Record<string, { old?: unknown; new?: unknown }>;
    return Object.entries(parsed).map(([field, change]) => ({
      field,
      oldValue: change?.old,
      newValue: change?.new,
      isSystemField: systemFields.has(field),
    }));
  } catch {
    return [];
  }
}

function formatChangeValue(log: AuditLogDto, field: string, value: unknown): string {
  if (value === null || value === undefined) return "Empty";

  if (typeof value === "boolean") {
    return value ? "Yes" : "No";
  }

  if (typeof value === "number") {
    const mapped = enumMaps[log.tableName]?.[field]?.[String(value)];
    return mapped ?? String(value);
  }

  if (typeof value === "string") {
    const mapped = enumMaps[log.tableName]?.[field]?.[value];
    if (mapped) return mapped;
    if (looksLikeIsoDate(value)) return new Date(value).toLocaleString();
    return value;
  }

  return JSON.stringify(value);
}

function AuditChangeDetails({
  log,
  showSystemFields,
}: {
  log: AuditLogDto;
  showSystemFields: boolean;
}) {
  const parsedChanges = parseChanges(log.changesJson);
  const visibleChanges = showSystemFields ? parsedChanges : parsedChanges.filter((change) => !change.isSystemField);
  const hiddenCount = parsedChanges.length - visibleChanges.length;

  if (parsedChanges.length === 0) {
    return (
      <details>
        <summary className="cursor-pointer text-sm text-[var(--link)] hover:underline">View Raw</summary>
        <pre className="mt-2 max-w-xl overflow-auto rounded-md border border-zinc-200 bg-zinc-50 p-2 text-xs text-zinc-800 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-100">
          {log.changesJson}
        </pre>
      </details>
    );
  }

  return (
    <details>
      <summary className="cursor-pointer text-sm text-[var(--link)] hover:underline">
        View Changes ({visibleChanges.length}{hiddenCount > 0 && !showSystemFields ? ` shown, ${hiddenCount} hidden` : ""})
      </summary>
      <div className="mt-2 overflow-auto rounded-md border border-zinc-200 dark:border-zinc-800">
        <table className="w-full text-xs">
          <thead className="bg-zinc-50 dark:bg-zinc-900">
            <tr>
              <th className="px-3 py-2 text-left font-semibold text-zinc-500">Field</th>
              <th className="px-3 py-2 text-left font-semibold text-zinc-500">Old</th>
              <th className="px-3 py-2 text-left font-semibold text-zinc-500">New</th>
            </tr>
          </thead>
          <tbody>
            {visibleChanges.map((change) => (
              <tr key={change.field} className="border-t border-zinc-100 dark:border-zinc-800">
                <td className="px-3 py-2 font-medium">{humanizeField(change.field)}</td>
                <td className="px-3 py-2 text-zinc-500">{formatChangeValue(log, change.field, change.oldValue)}</td>
                <td className="px-3 py-2">{formatChangeValue(log, change.field, change.newValue)}</td>
              </tr>
            ))}
            {visibleChanges.length === 0 ? (
              <tr>
                <td className="px-3 py-3 text-zinc-500" colSpan={3}>
                  Only system-maintained fields changed for this row. Enable system fields to inspect them.
                </td>
              </tr>
            ) : null}
          </tbody>
        </table>
      </div>
      <details className="mt-2">
        <summary className="cursor-pointer text-xs text-zinc-500 hover:underline">View Raw JSON</summary>
        <pre className="mt-2 max-w-xl overflow-auto rounded-md border border-zinc-200 bg-zinc-50 p-2 text-xs text-zinc-800 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-100">
          {log.changesJson}
        </pre>
      </details>
    </details>
  );
}

export function AuditLogTable({ logs }: { logs: AuditLogDto[] }) {
  const [showTechnical, setShowTechnical] = useState(false);
  const [showSystemFields, setShowSystemFields] = useState(false);
  const [search, setSearch] = useState("");

  const normalizedSearch = search.trim().toLowerCase();
  const visibleLogs = logs.filter((log) => {
    if (!showTechnical && log.isTechnical) {
      return false;
    }

    if (!normalizedSearch) {
      return true;
    }

    return [
      log.tableLabel,
      log.tableName,
      log.key,
      log.userLabel ?? "",
      log.userId ?? "",
      actionLabel[log.action] ?? String(log.action),
    ]
      .join(" ")
      .toLowerCase()
      .includes(normalizedSearch);
  });

  return (
    <Card>
      <div className="mb-4 flex flex-col gap-3 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <div className="text-sm font-semibold">Recent</div>
          <div className="mt-1 text-sm text-zinc-500">
            Readable field-level audit history. Technical sequence rows are hidden by default.
          </div>
        </div>

        <div className="grid gap-3 lg:min-w-[32rem] lg:grid-cols-[minmax(0,1fr)_auto_auto]">
          <Input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search table, action, key, or user" />
          <label className="inline-flex items-center gap-2 text-sm">
            <input type="checkbox" checked={showTechnical} onChange={(e) => setShowTechnical(e.target.checked)} />
            Show technical
          </label>
          <label className="inline-flex items-center gap-2 text-sm">
            <input type="checkbox" checked={showSystemFields} onChange={(e) => setShowSystemFields(e.target.checked)} />
            Show system fields
          </label>
        </div>
      </div>

      <div className="mb-3 text-xs text-zinc-500">
        Showing {visibleLogs.length} of {logs.length} log rows.
      </div>

      <div className="overflow-auto">
        <Table>
          <thead>
            <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
              <th className="py-2 pr-3">When</th>
              <th className="py-2 pr-3">User</th>
              <th className="py-2 pr-3">Record</th>
              <th className="py-2 pr-3">Action</th>
              <th className="py-2 pr-3">Key</th>
              <th className="py-2 pr-3">Changes</th>
            </tr>
          </thead>
          <tbody>
            {visibleLogs.map((log) => (
              <tr key={log.id} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                <td className="py-2 pr-3 text-zinc-500">{new Date(log.occurredAt).toLocaleString()}</td>
                <td className="py-2 pr-3">
                  <div>{log.userLabel ?? (log.userId ? log.userId.slice(0, 8) : "-")}</div>
                  {log.userId ? (
                    <div className="font-mono text-xs text-zinc-500">{log.userId.slice(0, 8)}</div>
                  ) : null}
                </td>
                <td className="py-2 pr-3">
                  <div>{log.tableLabel}</div>
                  {log.isTechnical ? <div className="text-xs text-amber-600 dark:text-amber-400">Technical</div> : null}
                </td>
                <td className="py-2 pr-3">{actionLabel[log.action] ?? log.action}</td>
                <td className="py-2 pr-3 font-mono text-xs text-zinc-600 dark:text-zinc-400">{log.key}</td>
                <td className="py-2 pr-3">
                  <AuditChangeDetails log={log} showSystemFields={showSystemFields} />
                </td>
              </tr>
            ))}
            {visibleLogs.length === 0 ? (
              <tr>
                <td className="py-6 text-sm text-zinc-500" colSpan={6}>
                  No audit logs match the current filters.
                </td>
              </tr>
            ) : null}
          </tbody>
        </Table>
      </div>
    </Card>
  );
}
