"use client";

import { useEffect, useState } from "react";
import { Card, Input, SecondaryButton, Select } from "@/components/ui";
import { LedgerAccountsClassicView } from "./LedgerAccountsClassicView";
import { LedgerAccountsGrid } from "./LedgerAccountsGrid";
import { type LedgerAccountDto, ledgerAccountTypeLabel, ledgerAccountTypeOptions } from "./types";

const searchInputClass =
  "min-w-[15rem] rounded-xl border-[var(--table-grid-strong)] bg-white/90 shadow-none dark:bg-[var(--surface)]";
const selectClass =
  "min-w-[10rem] rounded-xl border-[var(--table-grid-strong)] bg-white/90 shadow-none dark:bg-[var(--surface)]";
const workspaceStorageKey = "iss_finance_accounts_workspace_mode_v1";

type WorkspaceMode = "classic" | "priority";
type StatusFilter = "active" | "inactive" | "all";

function statValue(value: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(value);
}

function matchesSearch(account: LedgerAccountDto, term: string): boolean {
  if (!term) {
    return true;
  }

  const haystack = [
    account.code,
    account.name,
    account.description ?? "",
    account.parentAccountCode ?? "",
    account.parentAccountName ?? "",
    ledgerAccountTypeLabel(account.accountType),
  ]
    .join(" ")
    .toLowerCase();

  return haystack.includes(term);
}

function modeButtonClass(active: boolean): string {
  return [
    "inline-flex flex-1 items-center justify-center rounded-xl px-4 py-2 text-sm font-semibold transition",
    active
      ? "bg-[var(--accent)] text-[var(--accent-contrast)] shadow-[var(--shadow-button)]"
      : "text-[var(--foreground)] hover:bg-[var(--surface-soft)]",
  ].join(" ");
}

export function LedgerAccountsWorkspace({ accounts }: { accounts: LedgerAccountDto[] }) {
  const [workspaceMode, setWorkspaceMode] = useState<WorkspaceMode>("classic");
  const [hasLoadedPreference, setHasLoadedPreference] = useState(false);
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("active");
  const [typeFilter, setTypeFilter] = useState<string>("all");

  useEffect(() => {
    const stored = window.localStorage.getItem(workspaceStorageKey);
    if (stored === "classic" || stored === "priority") {
      setWorkspaceMode(stored);
    }

    setHasLoadedPreference(true);
  }, []);

  useEffect(() => {
    if (!hasLoadedPreference) {
      return;
    }

    window.localStorage.setItem(workspaceStorageKey, workspaceMode);
  }, [hasLoadedPreference, workspaceMode]);

  const normalizedSearch = search.trim().toLowerCase();
  const filteredAccounts = accounts.filter((account) => {
    if (!matchesSearch(account, normalizedSearch)) {
      return false;
    }

    if (statusFilter === "active" && !account.isActive) {
      return false;
    }

    if (statusFilter === "inactive" && account.isActive) {
      return false;
    }

    if (typeFilter !== "all" && String(account.accountType) !== typeFilter) {
      return false;
    }

    return true;
  });

  const activeCount = accounts.filter((account) => account.isActive).length;
  const inactiveCount = accounts.length - activeCount;
  const postingCount = accounts.filter((account) => account.allowsPosting).length;

  return (
    <div className="space-y-6">
      <Card className="space-y-4">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
          <div>
            <div className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--muted-foreground)]">
              Workspace mode
            </div>
            <h2 className="mt-2 text-xl font-semibold text-[var(--foreground)]">Choose how users work the ledger</h2>
            <p className="mt-2 max-w-3xl text-sm leading-6 text-[var(--muted-foreground)]">
              Priority-style systems support dense form-style grids, but established ERP teams usually keep the classic form and row workflow available too. This workspace keeps both patterns and remembers the{" "}
              user&apos;s last choice on this device.
            </p>
          </div>

          <div className="inline-flex w-full max-w-md rounded-2xl border border-[var(--input-border)] bg-[var(--surface)] p-1 shadow-[var(--shadow-control)]">
            <button
              type="button"
              className={modeButtonClass(workspaceMode === "classic")}
              onClick={() => setWorkspaceMode("classic")}
              aria-pressed={workspaceMode === "classic"}
            >
              Classic
            </button>
            <button
              type="button"
              className={modeButtonClass(workspaceMode === "priority")}
              onClick={() => setWorkspaceMode("priority")}
              aria-pressed={workspaceMode === "priority"}
            >
              Priority Grid
            </button>
          </div>
        </div>

        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
          <div className="rounded-2xl border border-[var(--table-grid)] bg-[var(--surface)] px-4 py-3">
            <div className="text-[11px] font-semibold uppercase tracking-[0.2em] text-[var(--muted-foreground)]">
              Active
            </div>
            <div className="mt-2 text-2xl font-semibold text-[var(--foreground)]">{statValue(activeCount)}</div>
          </div>
          <div className="rounded-2xl border border-[var(--table-grid)] bg-[var(--surface)] px-4 py-3">
            <div className="text-[11px] font-semibold uppercase tracking-[0.2em] text-[var(--muted-foreground)]">
              Posting
            </div>
            <div className="mt-2 text-2xl font-semibold text-[var(--foreground)]">{statValue(postingCount)}</div>
          </div>
          <div className="rounded-2xl border border-[var(--table-grid)] bg-[var(--surface)] px-4 py-3">
            <div className="text-[11px] font-semibold uppercase tracking-[0.2em] text-[var(--muted-foreground)]">
              Inactive
            </div>
            <div className="mt-2 text-2xl font-semibold text-[var(--foreground)]">{statValue(inactiveCount)}</div>
          </div>
          <div className="rounded-2xl border border-[var(--table-grid)] bg-[var(--surface)] px-4 py-3">
            <div className="text-[11px] font-semibold uppercase tracking-[0.2em] text-[var(--muted-foreground)]">
              Visible
            </div>
            <div className="mt-2 text-2xl font-semibold text-[var(--foreground)]">{statValue(filteredAccounts.length)}</div>
          </div>
        </div>

        <div className="flex flex-col gap-3 xl:flex-row xl:items-end xl:justify-between">
          <div className="flex flex-wrap items-end gap-3">
            <div>
              <label className="mb-1 block text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted-foreground)]">
                Search
              </label>
              <Input
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Code, name, parent, description..."
                className={searchInputClass}
              />
            </div>
            <div>
              <label className="mb-1 block text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted-foreground)]">
                Type
              </label>
              <Select value={typeFilter} onChange={(e) => setTypeFilter(e.target.value)} className={selectClass}>
                <option value="all">All types</option>
                {ledgerAccountTypeOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </Select>
            </div>
            <div>
              <label className="mb-1 block text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted-foreground)]">
                Status
              </label>
              <Select
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value as StatusFilter)}
                className={selectClass}
              >
                <option value="active">Active only</option>
                <option value="inactive">Inactive only</option>
                <option value="all">All accounts</option>
              </Select>
            </div>
          </div>

          <SecondaryButton
            type="button"
            onClick={() => {
              setSearch("");
              setTypeFilter("all");
              setStatusFilter("active");
            }}
          >
            Reset to Active
          </SecondaryButton>
        </div>

        <div className="rounded-2xl border border-[var(--table-grid)] bg-[var(--surface-soft)] px-4 py-3 text-xs leading-6 text-[var(--muted-foreground)]">
          {workspaceMode === "classic"
            ? "Classic keeps the dedicated create form and lets users edit by clicking a row or using the Edit action."
            : "Priority Grid keeps users in the table, with inline row creation, dense headers, and explicit save or cancel actions."}
        </div>
      </Card>

      {workspaceMode === "classic" ? (
        <LedgerAccountsClassicView accounts={accounts} filteredAccounts={filteredAccounts} />
      ) : (
        <LedgerAccountsGrid accounts={accounts} filteredAccounts={filteredAccounts} />
      )}
    </div>
  );
}
