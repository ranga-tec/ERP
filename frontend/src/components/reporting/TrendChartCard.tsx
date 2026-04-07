"use client";

import { useState } from "react";
import { Card } from "@/components/ui";

type TrendPoint = {
  periodStart: string;
  label: string;
  amount: number;
  count: number;
};

type MetricKey = "amount" | "count";

function buttonClass(active: boolean) {
  return [
    "rounded-full border px-3 py-1.5 text-xs font-semibold uppercase tracking-[0.18em] transition",
    active
      ? "border-[var(--link)] bg-[var(--accent-muted)] text-[var(--link)]"
      : "border-[var(--input-border)] bg-[var(--surface)] text-[var(--muted-foreground)] hover:border-[var(--link)]/30 hover:text-[var(--foreground)]",
  ].join(" ");
}

function barColor(index: number) {
  const palette = [
    "bg-[color:color-mix(in_srgb,var(--accent)_82%,white_18%)]",
    "bg-[color:color-mix(in_srgb,var(--link)_72%,white_28%)]",
    "bg-[color:color-mix(in_srgb,#0f766e_76%,white_24%)]",
    "bg-[color:color-mix(in_srgb,#b45309_72%,white_28%)]",
    "bg-[color:color-mix(in_srgb,#be123c_72%,white_28%)]",
    "bg-[color:color-mix(in_srgb,#4f46e5_70%,white_30%)]",
  ];

  return palette[index % palette.length];
}

export function TrendChartCard({
  title,
  description,
  points,
  locale,
  currencyCode,
  amountLabel,
  countLabel,
}: {
  title: string;
  description: string;
  points: TrendPoint[];
  locale: string;
  currencyCode: string;
  amountLabel: string;
  countLabel: string;
}) {
  const [metric, setMetric] = useState<MetricKey>("amount");

  const max = Math.max(
    1,
    ...points.map((point) => Math.max(0, metric === "amount" ? Math.abs(point.amount) : point.count)),
  );

  const formatValue = (point: TrendPoint) => {
    if (metric === "amount") {
      try {
        return new Intl.NumberFormat(locale, {
          style: "currency",
          currency: currencyCode,
          maximumFractionDigits: 2,
        }).format(point.amount);
      } catch {
        return new Intl.NumberFormat("en-US", {
          style: "currency",
          currency: "USD",
          maximumFractionDigits: 2,
        }).format(point.amount);
      }
    }

    return new Intl.NumberFormat(locale, { maximumFractionDigits: 0 }).format(point.count);
  };

  return (
    <Card className="space-y-4">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <div className="text-sm font-semibold">{title}</div>
          <p className="mt-1 text-sm text-zinc-500">{description}</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <button type="button" className={buttonClass(metric === "amount")} onClick={() => setMetric("amount")}>
            {amountLabel}
          </button>
          <button type="button" className={buttonClass(metric === "count")} onClick={() => setMetric("count")}>
            {countLabel}
          </button>
        </div>
      </div>

      <div className="space-y-3">
        {points.map((point, index) => {
          const rawValue = metric === "amount" ? Math.abs(point.amount) : point.count;
          const width = `${Math.max((rawValue / max) * 100, rawValue > 0 ? 6 : 0)}%`;

          return (
            <div key={point.periodStart} className="rounded-xl border border-[var(--card-border)] bg-[var(--surface)] px-3 py-3">
              <div className="flex items-center justify-between gap-3">
                <div className="text-sm font-medium text-[var(--foreground)]">{point.label}</div>
                <div className="text-sm font-semibold text-[var(--foreground)]">{formatValue(point)}</div>
              </div>
              <div className="mt-2 h-2.5 overflow-hidden rounded-full bg-[var(--accent-muted)]">
                <div className={["h-full rounded-full transition-[width]", barColor(index)].join(" ")} style={{ width }} />
              </div>
            </div>
          );
        })}
      </div>
    </Card>
  );
}
