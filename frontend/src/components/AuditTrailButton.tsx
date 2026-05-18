"use client";

import { useMemo, useState } from "react";
import { apiGet } from "@/lib/api-client";

type AuditLogDto = {
  id: string;
  occurredAt: string;
  userLabel?: string | null;
  userId?: string | null;
  tableName: string;
  tableLabel: string;
  action: number;
  key: string;
};

type AuditTrailButtonProps = {
  tableName: string;
  recordId: string;
  label?: string;
};

const actionLabel: Record<number, string> = {
  1: "Created",
  2: "Updated",
  3: "Deleted",
};

function formatDate(value?: string): string {
  return value ? new Date(value).toLocaleString() : "-";
}

function actor(log?: AuditLogDto): string {
  return log?.userLabel ?? log?.userId ?? "System";
}

export function AuditTrailButton({ tableName, recordId, label = "Audit" }: AuditTrailButtonProps) {
  const [open, setOpen] = useState(false);
  const [logs, setLogs] = useState<AuditLogDto[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const created = useMemo(() => {
    const inserts = logs?.filter((log) => log.action === 1) ?? [];
    return inserts[inserts.length - 1];
  }, [logs]);

  const lastChange = logs?.find((log) => log.action === 2) ?? logs?.[0];

  async function toggle() {
    const nextOpen = !open;
    setOpen(nextOpen);
    if (!nextOpen || logs || loading) return;

    setLoading(true);
    setError(null);
    try {
      const params = new URLSearchParams({ tableName, key: recordId, take: "25" });
      const result = await apiGet<AuditLogDto[]>(`audit-logs?${params.toString()}`);
      setLogs(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unable to load audit history.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <span className="relative inline-flex">
      <button
        type="button"
        onClick={() => void toggle()}
        aria-expanded={open}
        title={label}
        className="inline-flex h-7 w-7 items-center justify-center rounded-md border border-[var(--input-border)] bg-[var(--surface)] text-[var(--muted-foreground)] shadow-[var(--shadow-control)] transition hover:bg-[var(--surface-soft)] hover:text-[var(--foreground)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)]"
      >
        <span className="text-[11px] font-semibold" aria-hidden="true">i</span>
        <span className="sr-only">{label}</span>
      </button>
      {open ? (
        <div className="absolute right-0 top-8 z-30 w-80 rounded-lg border border-[var(--card-border)] bg-[var(--card-bg)] p-3 text-left shadow-xl">
          <div className="mb-2 flex items-start justify-between gap-3">
            <div>
              <div className="text-sm font-semibold">Audit Trail</div>
              <div className="text-xs text-[var(--muted-foreground)]">{tableName}</div>
            </div>
            <a className="text-xs font-semibold text-[var(--link)] hover:text-[var(--link-hover)]" href={`/audit-logs?search=${encodeURIComponent(recordId)}`}>
              Full Log
            </a>
          </div>
          {loading ? <div className="text-sm text-[var(--muted-foreground)]">Loading audit history...</div> : null}
          {error ? <div className="text-sm text-red-600">{error}</div> : null}
          {!loading && !error ? (
            logs && logs.length > 0 ? (
              <div className="space-y-3">
                <div className="rounded-md border border-zinc-200 p-2 dark:border-zinc-800">
                  <div className="text-xs font-semibold uppercase text-[var(--muted-foreground)]">Created</div>
                  <div className="mt-1 text-sm">{formatDate(created?.occurredAt)}</div>
                  <div className="text-xs text-[var(--muted-foreground)]">by {actor(created)}</div>
                </div>
                <div className="rounded-md border border-zinc-200 p-2 dark:border-zinc-800">
                  <div className="text-xs font-semibold uppercase text-[var(--muted-foreground)]">Last Updated</div>
                  <div className="mt-1 text-sm">{formatDate(lastChange?.occurredAt)}</div>
                  <div className="text-xs text-[var(--muted-foreground)]">by {actor(lastChange)}</div>
                </div>
                <div className="max-h-48 overflow-auto border-t border-zinc-200 pt-2 text-xs dark:border-zinc-800">
                  {logs.slice(0, 6).map((log) => (
                    <div key={log.id} className="flex justify-between gap-3 py-1">
                      <span className="font-medium">{actionLabel[log.action] ?? `Action ${log.action}`}</span>
                      <span className="text-right text-[var(--muted-foreground)]">
                        {formatDate(log.occurredAt)}
                        <br />
                        {actor(log)}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            ) : (
              <div className="text-sm text-[var(--muted-foreground)]">No audit history found for this record.</div>
            )
          ) : null}
        </div>
      ) : null}
    </span>
  );
}
