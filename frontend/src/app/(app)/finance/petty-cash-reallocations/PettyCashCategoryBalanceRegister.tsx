"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { Card, Input, Table } from "@/components/ui";
import { describeCategory, money, type ReallocationCandidateDto } from "./types";

type CategoryFilter = "all" | "job-wise" | "other";

export function PettyCashCategoryBalanceRegister({ rows }: { rows: ReallocationCandidateDto[] }) {
  const [search, setSearch] = useState("");
  const [fundId, setFundId] = useState("all");
  const [categoryFilter, setCategoryFilter] = useState<CategoryFilter>("all");
  const funds = useMemo(
    () => Array.from(new Map(rows.map((row) => [row.pettyCashFundId, row.pettyCashFundCode])).entries()),
    [rows],
  );
  const filteredRows = useMemo(() => {
    const term = search.trim().toLocaleLowerCase();
    return rows.filter((row) => {
      if (fundId !== "all" && row.pettyCashFundId !== fundId) return false;
      if (categoryFilter === "job-wise" && row.category !== 1) return false;
      if (categoryFilter === "other" && row.category === 1) return false;
      if (!term) return true;
      return [
        row.pettyCashFundCode,
        row.requestNumber,
        row.serviceJobNumber,
        row.customCategoryName,
        row.purpose,
        describeCategory(row.category, row.serviceJobNumber, row.customCategoryName),
      ].some((value) => value?.toLocaleLowerCase().includes(term));
    });
  }, [categoryFilter, fundId, rows, search]);
  const totals = useMemo(() => filteredRows.reduce(
    (result, row) => ({
      funded: result.funded + row.fundedAmount,
      ledger: result.ledger + row.ledgerBalance,
      reserved: result.reserved + row.pendingReturnAmount + row.pendingReallocationAmount,
      available: result.available + row.availableBalance,
    }),
    { funded: 0, ledger: 0, reserved: 0, available: 0 },
  ), [filteredRows]);

  return (
    <Card>
      <div className="space-y-4">
        <div>
          <h2 className="text-lg font-semibold">Category Fund Balances</h2>
          <p className="mt-1 text-sm text-zinc-500">
            Review remaining authorization by job and category before preparing a reallocation.
          </p>
        </div>

        <div className="grid gap-3 md:grid-cols-3">
          <Input
            aria-label="Search category balances"
            placeholder="Search job, category, request, or purpose"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
          <select
            aria-label="Filter by petty cash fund"
            className="w-full rounded-md border border-[var(--input-border)] bg-[var(--surface)] px-2.5 py-2 text-sm"
            value={fundId}
            onChange={(event) => setFundId(event.target.value)}
          >
            <option value="all">All petty cash funds</option>
            {funds.map(([id, code]) => <option key={id} value={id}>{code}</option>)}
          </select>
          <select
            aria-label="Filter by category type"
            className="w-full rounded-md border border-[var(--input-border)] bg-[var(--surface)] px-2.5 py-2 text-sm"
            value={categoryFilter}
            onChange={(event) => setCategoryFilter(event.target.value as CategoryFilter)}
          >
            <option value="all">All categories</option>
            <option value="job-wise">Job Wise only</option>
            <option value="other">Non-job categories</option>
          </select>
        </div>

        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
          <BalanceSummary label="Original funded" value={totals.funded} />
          <BalanceSummary label="Current ledger balance" value={totals.ledger} />
          <BalanceSummary label="Pending reservations" value={totals.reserved} />
          <BalanceSummary label="Available to spend" value={totals.available} emphasize />
        </div>

        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Job / Category</th>
                <th className="py-2 pr-3">Fund</th>
                <th className="py-2 pr-3">Request</th>
                <th className="py-2 pr-3">Purpose</th>
                <th className="py-2 pr-3 text-right">Funded</th>
                <th className="py-2 pr-3 text-right">Ledger</th>
                <th className="py-2 pr-3 text-right">Reserved</th>
                <th className="py-2 text-right">Available</th>
              </tr>
            </thead>
            <tbody>
              {filteredRows.map((row) => {
                const reserved = row.pendingReturnAmount + row.pendingReallocationAmount;
                return (
                  <tr key={row.pettyCashRequestLineId} className="border-b border-zinc-100 dark:border-zinc-900">
                    <td className="py-2 pr-3">
                      <div className="font-medium">{describeCategory(row.category, row.serviceJobNumber, row.customCategoryName)}</div>
                      {row.category === 1 ? <div className="text-xs text-zinc-500">Job-specific allocation</div> : null}
                    </td>
                    <td className="py-2 pr-3">{row.pettyCashFundCode}</td>
                    <td className="py-2 pr-3">
                      <Link className="font-mono text-xs underline" href={`/finance/petty-cash-requests/${row.pettyCashRequestId}`}>
                        {row.requestNumber}
                      </Link>
                    </td>
                    <td className="max-w-64 py-2 pr-3 text-sm text-zinc-600 dark:text-zinc-400">{row.purpose}</td>
                    <td className="py-2 pr-3 text-right">{money(row.fundedAmount)}</td>
                    <td className="py-2 pr-3 text-right">{money(row.ledgerBalance)}</td>
                    <td className="py-2 pr-3 text-right" title={`Returns ${money(row.pendingReturnAmount)}; reallocations ${money(row.pendingReallocationAmount)}`}>
                      {money(reserved)}
                    </td>
                    <td className="py-2 text-right font-semibold">{money(row.availableBalance)}</td>
                  </tr>
                );
              })}
              {filteredRows.length === 0 ? (
                <tr><td colSpan={8} className="py-8 text-center text-sm text-zinc-500">No funded categories match these filters.</td></tr>
              ) : null}
            </tbody>
          </Table>
        </div>
        <p className="text-xs text-zinc-500">
          Reserved includes submitted returns and submitted reallocations. Available is the safe amount remaining after those reservations.
        </p>
      </div>
    </Card>
  );
}

function BalanceSummary({ label, value, emphasize = false }: { label: string; value: number; emphasize?: boolean }) {
  return (
    <div className="rounded-md border border-[var(--card-border)] bg-[var(--surface-soft)] p-3">
      <div className="text-xs uppercase tracking-wide text-zinc-500">{label}</div>
      <div className={`mt-1 text-lg font-semibold ${emphasize ? "text-emerald-700 dark:text-emerald-400" : ""}`}>{money(value)}</div>
    </div>
  );
}
