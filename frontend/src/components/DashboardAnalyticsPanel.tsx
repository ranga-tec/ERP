"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { Card } from "@/components/ui";

type DashboardMetricDto = {
  label: string;
  valueType: "count" | "currency";
  count?: number | null;
  amount?: number | null;
  description: string;
  href?: string | null;
};

type DashboardAlertDto = {
  title: string;
  description: string;
  severity: string;
  count: number;
  href?: string | null;
};

type DashboardSectionDto = {
  key: string;
  title: string;
  description: string;
  metrics: DashboardMetricDto[];
};

type AnalyticsTab = "queues" | "finance" | "alerts";
type AlertSeverityFilter = "all" | "high" | "medium" | "low";

function formatMetricValue(
  metric: DashboardMetricDto,
  locale: string,
  currencyCode: string,
) {
  if (metric.valueType === "currency") {
    try {
      return new Intl.NumberFormat(locale, {
        style: "currency",
        currency: currencyCode,
        maximumFractionDigits: 2,
      }).format(metric.amount ?? 0);
    } catch {
      return new Intl.NumberFormat("en-LK", {
        style: "currency",
        currency: "LKR",
        maximumFractionDigits: 2,
      }).format(metric.amount ?? 0);
    }
  }

  return new Intl.NumberFormat(locale, { maximumFractionDigits: 0 }).format(metric.count ?? 0);
}

function metricNumericValue(metric: DashboardMetricDto) {
  return metric.valueType === "currency"
    ? Math.max(0, Math.abs(metric.amount ?? 0))
    : Math.max(0, metric.count ?? 0);
}

function tabButtonClass(active: boolean) {
  return [
    "rounded-full border px-3 py-1.5 text-xs font-semibold uppercase tracking-[0.18em] transition",
    active
      ? "border-[var(--link)] bg-[var(--accent-muted)] text-[var(--link)]"
      : "border-[var(--input-border)] bg-[var(--surface)] text-[var(--muted-foreground)] hover:border-[var(--link)]/30 hover:text-[var(--foreground)]",
  ].join(" ");
}

function filterButtonClass(active: boolean) {
  return [
    "rounded-full border px-3 py-1.5 text-xs font-medium transition",
    active
      ? "border-[var(--link)] bg-[var(--accent-muted)] text-[var(--link)]"
      : "border-[var(--input-border)] bg-[var(--surface)] text-[var(--muted-foreground)] hover:border-[var(--link)]/25 hover:text-[var(--foreground)]",
  ].join(" ");
}

function chartBarColor(index: number) {
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

function alertSegmentColor(severity: string) {
  switch (severity) {
    case "high":
      return "bg-rose-500";
    case "medium":
      return "bg-amber-500";
    default:
      return "bg-sky-500";
  }
}

export function DashboardAnalyticsPanel({
  heroMetrics,
  alerts,
  sections,
  locale,
  currencyCode,
}: {
  heroMetrics: DashboardMetricDto[];
  alerts: DashboardAlertDto[];
  sections: DashboardSectionDto[];
  locale: string;
  currencyCode: string;
}) {
  const [activeTab, setActiveTab] = useState<AnalyticsTab>("queues");
  const [selectedSectionKey, setSelectedSectionKey] = useState("");
  const [alertFilter, setAlertFilter] = useState<AlertSeverityFilter>("all");

  const queueSections = useMemo(
    () =>
      sections
        .map((section) => ({
          ...section,
          metrics: section.metrics.filter((metric) => metric.valueType === "count"),
        }))
        .filter((section) => section.metrics.length > 0),
    [sections],
  );

  const financeMetrics = useMemo(() => {
    const sectionMetrics = sections.flatMap((section) =>
      section.metrics
        .filter((metric) => metric.valueType === "currency")
        .map((metric) => ({
          ...metric,
          sectionTitle: section.title,
          key: `${section.key}-${metric.label}-${metric.href ?? "na"}`,
        })),
    );

    if (sectionMetrics.length > 0) {
      return sectionMetrics;
    }

    return heroMetrics
      .filter((metric) => metric.valueType === "currency")
      .map((metric) => ({
        ...metric,
        sectionTitle: "Dashboard",
        key: `hero-${metric.label}-${metric.href ?? "na"}`,
      }));
  }, [heroMetrics, sections]);

  const effectiveSelectedSectionKey = queueSections.some((section) => section.key === selectedSectionKey)
    ? selectedSectionKey
    : queueSections[0]?.key ?? "";
  const selectedQueueSection =
    queueSections.find((section) => section.key === effectiveSelectedSectionKey) ?? null;

  const queueMax = Math.max(
    1,
    ...((selectedQueueSection?.metrics ?? []).map((metric) => metricNumericValue(metric))),
  );

  const financeMax = Math.max(1, ...(financeMetrics.map((metric) => metricNumericValue(metric))));

  const alertSummary = useMemo(() => {
    const seed = {
      high: 0,
      medium: 0,
      low: 0,
      total: 0,
    };

    return alerts.reduce((acc, alert) => {
      const severity = alert.severity === "high" || alert.severity === "medium" ? alert.severity : "low";
      acc[severity] += alert.count;
      acc.total += alert.count;
      return acc;
    }, seed);
  }, [alerts]);

  const filteredAlerts = alerts.filter((alert) =>
    alertFilter === "all"
      ? true
      : (alert.severity === "high" || alert.severity === "medium" ? alert.severity : "low") === alertFilter,
  );

  const tabs: Array<{ key: AnalyticsTab; label: string; enabled: boolean }> = [
    { key: "queues", label: "Queues", enabled: queueSections.length > 0 },
    { key: "finance", label: "Finance", enabled: financeMetrics.length > 0 },
    { key: "alerts", label: "Alerts", enabled: alerts.length > 0 },
  ];

  return (
    <Card className="overflow-hidden border-[var(--card-border)] bg-[var(--card-bg)]">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <div className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--muted-foreground)]">
            Interactive analytics
          </div>
          <h2 className="mt-2 text-xl font-semibold tracking-tight text-[var(--foreground)]">
            Dashboard analytics
          </h2>
          <p className="mt-2 max-w-3xl text-sm leading-6 text-[var(--muted-foreground)]">
            Switch between work queues, finance exposure, and exception load to explore the live dashboard numbers.
          </p>
        </div>

        <div className="flex flex-wrap gap-2">
          {tabs.map((tab) => (
            <button
              key={tab.key}
              type="button"
              disabled={!tab.enabled}
              className={[tabButtonClass(activeTab === tab.key), !tab.enabled ? "cursor-not-allowed opacity-45" : ""].join(" ")}
              onClick={() => setActiveTab(tab.key)}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </div>

      <div className="mt-6">
        {activeTab === "queues" ? (
          queueSections.length > 0 ? (
            <div className="grid gap-5 xl:grid-cols-[16rem_minmax(0,1fr)]">
              <div className="space-y-2">
                <div className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted-foreground)]">
                  Workstreams
                </div>
                {queueSections.map((section) => {
                  const sectionTotal = section.metrics.reduce((sum, metric) => sum + metricNumericValue(metric), 0);
                  const active = effectiveSelectedSectionKey === section.key;
                  return (
                    <button
                      key={section.key}
                      type="button"
                      className={[
                        "w-full rounded-2xl border px-4 py-3 text-left transition",
                        active
                          ? "border-[var(--link)] bg-[var(--accent-muted)]"
                          : "border-[var(--card-border)] bg-[var(--surface)] hover:border-[var(--link)]/30 hover:bg-[var(--surface-soft)]",
                      ].join(" ")}
                      onClick={() => setSelectedSectionKey(section.key)}
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <div className="text-sm font-semibold text-[var(--foreground)]">{section.title}</div>
                          <div className="mt-1 text-xs text-[var(--muted-foreground)]">{section.metrics.length} tracked metrics</div>
                        </div>
                        <div className="rounded-full border border-current/15 px-2 py-1 text-xs font-semibold text-[var(--foreground)]">
                          {new Intl.NumberFormat(locale, { maximumFractionDigits: 0 }).format(sectionTotal)}
                        </div>
                      </div>
                    </button>
                  );
                })}
              </div>

              {selectedQueueSection ? (
                <div className="space-y-4">
                  <div>
                    <div className="text-sm font-semibold text-[var(--foreground)]">{selectedQueueSection.title}</div>
                    <p className="mt-1 text-sm leading-6 text-[var(--muted-foreground)]">
                      {selectedQueueSection.description}
                    </p>
                  </div>

                  <div className="space-y-3">
                    {selectedQueueSection.metrics.map((metric, index) => {
                      const width = `${Math.max((metricNumericValue(metric) / queueMax) * 100, metricNumericValue(metric) > 0 ? 6 : 0)}%`;
                      const content = (
                        <div className="rounded-2xl border border-[var(--card-border)] bg-[var(--surface)] px-4 py-3 transition hover:border-[var(--link)]/25 hover:bg-[var(--surface-soft)]">
                          <div className="flex items-start justify-between gap-4">
                            <div className="min-w-0">
                              <div className="text-sm font-semibold text-[var(--foreground)]">{metric.label}</div>
                              <p className="mt-1 text-sm leading-6 text-[var(--muted-foreground)]">{metric.description}</p>
                            </div>
                            <div className="shrink-0 text-right">
                              <div className="text-lg font-semibold text-[var(--foreground)]">
                                {formatMetricValue(metric, locale, currencyCode)}
                              </div>
                              <div className="mt-1 text-xs uppercase tracking-[0.18em] text-[var(--muted-foreground)]">
                                {metric.href ? "Open queue" : "Live metric"}
                              </div>
                            </div>
                          </div>
                          <div className="mt-3 h-2.5 overflow-hidden rounded-full bg-[var(--accent-muted)]">
                            <div className={["h-full rounded-full transition-[width]", chartBarColor(index)].join(" ")} style={{ width }} />
                          </div>
                        </div>
                      );

                      return metric.href ? (
                        <Link key={`${selectedQueueSection.key}-${metric.label}`} href={metric.href}>
                          {content}
                        </Link>
                      ) : (
                        <div key={`${selectedQueueSection.key}-${metric.label}`}>{content}</div>
                      );
                    })}
                  </div>
                </div>
              ) : null}
            </div>
          ) : (
            <div className="text-sm text-[var(--muted-foreground)]">No queue analytics are available yet.</div>
          )
        ) : null}

        {activeTab === "finance" ? (
          financeMetrics.length > 0 ? (
            <div className="grid gap-5 xl:grid-cols-[minmax(0,1.2fr)_minmax(18rem,0.8fr)]">
              <div className="space-y-3">
                {financeMetrics.map((metric, index) => {
                  const width = `${Math.max((metricNumericValue(metric) / financeMax) * 100, metricNumericValue(metric) > 0 ? 8 : 0)}%`;
                  const content = (
                    <div className="rounded-2xl border border-[var(--card-border)] bg-[var(--surface)] px-4 py-3 transition hover:border-[var(--link)]/25 hover:bg-[var(--surface-soft)]">
                      <div className="flex items-start justify-between gap-4">
                        <div className="min-w-0">
                          <div className="text-sm font-semibold text-[var(--foreground)]">{metric.label}</div>
                          <div className="mt-1 text-xs uppercase tracking-[0.18em] text-[var(--muted-foreground)]">
                            {metric.sectionTitle}
                          </div>
                          <p className="mt-2 text-sm leading-6 text-[var(--muted-foreground)]">{metric.description}</p>
                        </div>
                        <div className="shrink-0 text-right">
                          <div className="text-lg font-semibold text-[var(--foreground)]">
                            {formatMetricValue(metric, locale, currencyCode)}
                          </div>
                          <div className="mt-1 text-xs uppercase tracking-[0.18em] text-[var(--muted-foreground)]">
                            {metric.href ? "Drill in" : "Read only"}
                          </div>
                        </div>
                      </div>
                      <div className="mt-3 h-2.5 overflow-hidden rounded-full bg-[var(--accent-muted)]">
                        <div className={["h-full rounded-full transition-[width]", chartBarColor(index)].join(" ")} style={{ width }} />
                      </div>
                    </div>
                  );

                  return metric.href ? (
                    <Link key={metric.key} href={metric.href}>
                      {content}
                    </Link>
                  ) : (
                    <div key={metric.key}>{content}</div>
                  );
                })}
              </div>

              <div className="space-y-3">
                <div className="rounded-2xl border border-[var(--card-border)] bg-[var(--surface)] px-4 py-4">
                  <div className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted-foreground)]">
                    Exposure summary
                  </div>
                  <div className="mt-4 space-y-4">
                    {financeMetrics.slice(0, 4).map((metric) => (
                      <div key={`${metric.key}-summary`} className="flex items-center justify-between gap-3">
                        <div className="min-w-0">
                          <div className="text-sm font-semibold text-[var(--foreground)]">{metric.label}</div>
                          <div className="text-xs text-[var(--muted-foreground)]">{metric.sectionTitle}</div>
                        </div>
                        <div className="text-sm font-semibold text-[var(--foreground)]">
                          {formatMetricValue(metric, locale, currencyCode)}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
                <div className="rounded-2xl border border-[var(--card-border)] bg-[var(--surface)] px-4 py-4">
                  <div className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted-foreground)]">
                    Read this as
                  </div>
                  <p className="mt-3 text-sm leading-6 text-[var(--muted-foreground)]">
                    The longest bars show where cash, liability, or balance-sheet attention is currently concentrated. Use
                    the row links to jump straight into the finance queue behind each figure.
                  </p>
                </div>
              </div>
            </div>
          ) : (
            <div className="text-sm text-[var(--muted-foreground)]">No finance analytics are available yet.</div>
          )
        ) : null}

        {activeTab === "alerts" ? (
          alerts.length > 0 ? (
            <div className="space-y-4">
              <div className="flex flex-wrap items-center gap-2">
                <button type="button" className={filterButtonClass(alertFilter === "all")} onClick={() => setAlertFilter("all")}>
                  All
                </button>
                <button type="button" className={filterButtonClass(alertFilter === "high")} onClick={() => setAlertFilter("high")}>
                  High
                </button>
                <button type="button" className={filterButtonClass(alertFilter === "medium")} onClick={() => setAlertFilter("medium")}>
                  Medium
                </button>
                <button type="button" className={filterButtonClass(alertFilter === "low")} onClick={() => setAlertFilter("low")}>
                  Low
                </button>
              </div>

              <div className="rounded-2xl border border-[var(--card-border)] bg-[var(--surface)] px-4 py-4">
                <div className="flex items-center justify-between gap-4">
                  <div>
                    <div className="text-sm font-semibold text-[var(--foreground)]">Alert severity mix</div>
                    <p className="mt-1 text-sm leading-6 text-[var(--muted-foreground)]">
                      Click a severity filter to isolate the exact exception cards below.
                    </p>
                  </div>
                  <div className="text-right">
                    <div className="text-xs uppercase tracking-[0.18em] text-[var(--muted-foreground)]">Total impact</div>
                    <div className="mt-1 text-2xl font-semibold text-[var(--foreground)]">
                      {new Intl.NumberFormat(locale, { maximumFractionDigits: 0 }).format(alertSummary.total)}
                    </div>
                  </div>
                </div>

                <div className="mt-4 flex h-4 overflow-hidden rounded-full bg-[var(--accent-muted)]">
                  {alertSummary.total > 0 ? (
                    <>
                      {(["high", "medium", "low"] as const).map((severity) => {
                        const count = alertSummary[severity];
                        if (count <= 0) {
                          return null;
                        }

                        return (
                          <button
                            key={severity}
                            type="button"
                            aria-label={`${severity} alerts`}
                            className={[alertSegmentColor(severity), "h-full transition-opacity hover:opacity-80"].join(" ")}
                            style={{ width: `${(count / alertSummary.total) * 100}%` }}
                            onClick={() => setAlertFilter(severity)}
                          />
                        );
                      })}
                    </>
                  ) : null}
                </div>

                <div className="mt-4 grid gap-3 sm:grid-cols-3">
                  {(["high", "medium", "low"] as const).map((severity) => (
                    <div key={severity} className="rounded-xl border border-[var(--card-border)] bg-[var(--card-bg)] px-3 py-3">
                      <div className="text-xs uppercase tracking-[0.18em] text-[var(--muted-foreground)]">{severity}</div>
                      <div className="mt-2 text-xl font-semibold text-[var(--foreground)]">
                        {new Intl.NumberFormat(locale, { maximumFractionDigits: 0 }).format(alertSummary[severity])}
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              <div className="grid gap-3 lg:grid-cols-2">
                {filteredAlerts.map((alert) => {
                  const severity = alert.severity === "high" || alert.severity === "medium" ? alert.severity : "low";
                  const content = (
                    <div className="rounded-2xl border border-[var(--card-border)] bg-[var(--surface)] px-4 py-4 transition hover:border-[var(--link)]/25 hover:bg-[var(--surface-soft)]">
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <div className="text-sm font-semibold text-[var(--foreground)]">{alert.title}</div>
                          <div className="mt-1 text-xs uppercase tracking-[0.18em] text-[var(--muted-foreground)]">
                            {severity}
                          </div>
                          <p className="mt-2 text-sm leading-6 text-[var(--muted-foreground)]">{alert.description}</p>
                        </div>
                        <div className="rounded-full border border-current/15 px-3 py-1 text-sm font-semibold text-[var(--foreground)]">
                          {new Intl.NumberFormat(locale, { maximumFractionDigits: 0 }).format(alert.count)}
                        </div>
                      </div>
                    </div>
                  );

                  return alert.href ? (
                    <Link key={`${alert.title}-${alert.href}`} href={alert.href}>
                      {content}
                    </Link>
                  ) : (
                    <div key={alert.title}>{content}</div>
                  );
                })}
              </div>
            </div>
          ) : (
            <div className="text-sm text-[var(--muted-foreground)]">No alert analytics are available yet.</div>
          )
        ) : null}
      </div>
    </Card>
  );
}
