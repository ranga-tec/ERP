import Link from "next/link";
import { cookies } from "next/headers";
import { Card, SecondaryLink } from "@/components/ui";
import { backendFetchJson } from "@/lib/backend.server";
import { ISS_TOKEN_COOKIE } from "@/lib/env";
import { sessionFromToken } from "@/lib/jwt";
import { canAccessPath } from "@/lib/route-access";
import { userSettingsFromCookies } from "@/lib/user-settings.server";

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

type DashboardQuickActionDto = {
  label: string;
  description: string;
  href: string;
};

type DashboardDto = {
  openServiceJobs: number;
  arOutstanding: number;
  apOutstanding: number;
  reorderAlerts: number;
  generatedAt: string;
  heroMetrics: DashboardMetricDto[];
  alerts: DashboardAlertDto[];
  sections: DashboardSectionDto[];
  quickActions: DashboardQuickActionDto[];
};

const FALLBACK_ACTIONS: DashboardQuickActionDto[] = [
  {
    label: "Purchase orders",
    description: "Review open buying documents and receiving progress.",
    href: "/procurement/purchase-orders",
  },
  {
    label: "Sales orders",
    description: "Follow confirmed customer demand into fulfilment.",
    href: "/sales/orders",
  },
  {
    label: "Service jobs",
    description: "Run the workshop queue and job execution.",
    href: "/service/jobs",
  },
  {
    label: "Reorder alerts",
    description: "Review stock that needs replenishment.",
    href: "/inventory/reorder-alerts",
  },
  {
    label: "Accounts receivable",
    description: "Chase collections and open customer balances.",
    href: "/finance/ar",
  },
  {
    label: "Reporting",
    description: "Open the reporting workspace and operational reports.",
    href: "/reporting",
  },
];

function formatMetricValue(
  metric: DashboardMetricDto,
  formatMoney: (value: number) => string,
): string {
  if (metric.valueType === "currency") {
    return formatMoney(metric.amount ?? 0);
  }

  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(metric.count ?? 0);
}

function alertToneClass(severity: string): string {
  switch (severity) {
    case "high":
      return "border-rose-200 bg-rose-50 text-rose-700 dark:border-rose-900/80 dark:bg-rose-950/40 dark:text-rose-200";
    case "medium":
      return "border-amber-200 bg-amber-50 text-amber-700 dark:border-amber-900/80 dark:bg-amber-950/40 dark:text-amber-200";
    default:
      return "border-zinc-200 bg-[var(--surface)] text-[var(--foreground)]";
  }
}

function MetricCard({
  metric,
  formatMoney,
}: {
  metric: DashboardMetricDto;
  formatMoney: (value: number) => string;
}) {
  const content = (
    <Card className="h-full border-[var(--card-border)] bg-[var(--card-bg)]">
      <div className="flex items-start justify-between gap-3">
        <div>
          <div className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--muted-foreground)]">
            {metric.label}
          </div>
          <div className="mt-3 text-3xl font-semibold tracking-tight text-[var(--foreground)]">
            {formatMetricValue(metric, formatMoney)}
          </div>
        </div>
        {metric.href ? (
          <span className="rounded-full border border-[var(--input-border)] bg-[var(--surface)] px-2 py-1 text-[11px] font-semibold uppercase tracking-[0.18em] text-[var(--muted-foreground)]">
            Open
          </span>
        ) : null}
      </div>
      <p className="mt-4 text-sm leading-6 text-[var(--muted-foreground)]">{metric.description}</p>
    </Card>
  );

  if (!metric.href) {
    return content;
  }

  return (
    <Link className="block transition hover:-translate-y-0.5" href={metric.href}>
      {content}
    </Link>
  );
}

function SectionMetricRow({
  metric,
  formatMoney,
}: {
  metric: DashboardMetricDto;
  formatMoney: (value: number) => string;
}) {
  const rowContent = (
    <div className="flex items-start justify-between gap-4 rounded-2xl border border-[var(--card-border)] bg-[var(--surface)] px-4 py-3 transition hover:border-[var(--link)]/25 hover:bg-[var(--surface-soft)]">
      <div className="min-w-0">
        <div className="text-sm font-semibold text-[var(--foreground)]">{metric.label}</div>
        <p className="mt-1 text-sm leading-6 text-[var(--muted-foreground)]">{metric.description}</p>
      </div>
      <div className="shrink-0 text-right">
        <div className="text-lg font-semibold text-[var(--foreground)]">
          {formatMetricValue(metric, formatMoney)}
        </div>
        <div className="mt-1 text-xs uppercase tracking-[0.18em] text-[var(--muted-foreground)]">
          {metric.href ? "Drill in" : "Read only"}
        </div>
      </div>
    </div>
  );

  if (!metric.href) {
    return rowContent;
  }

  return <Link href={metric.href}>{rowContent}</Link>;
}

function QuickActionCard({ action }: { action: DashboardQuickActionDto }) {
  return (
    <Link href={action.href}>
      <Card className="h-full border-[var(--card-border)] bg-[var(--card-bg)] transition hover:-translate-y-0.5 hover:border-[var(--link)]/25 hover:bg-[var(--surface-soft)]">
        <div className="text-base font-semibold text-[var(--foreground)]">{action.label}</div>
        <p className="mt-2 text-sm leading-6 text-[var(--muted-foreground)]">{action.description}</p>
      </Card>
    </Link>
  );
}

export default async function DashboardPage() {
  const settings = await userSettingsFromCookies();
  const cookieStore = await cookies();
  const token = cookieStore.get(ISS_TOKEN_COOKIE)?.value;
  const session = token ? sessionFromToken(token) : null;

  const fallbackQuickActions = FALLBACK_ACTIONS.filter((action) =>
    canAccessPath(session?.roles ?? [], action.href),
  );

  let dashboard: DashboardDto | null = null;
  let dashboardError: string | null = null;
  try {
    dashboard = await backendFetchJson<DashboardDto>("/reporting/dashboard");
  } catch {
    dashboardError = "Live dashboard data is temporarily unavailable. Operational shortcuts are still available below.";
  }

  const formatMoney = (value: number) => {
    try {
      return new Intl.NumberFormat(settings.locale, {
        style: "currency",
        currency: settings.baseCurrencyCode,
        maximumFractionDigits: 2,
      }).format(value);
    } catch {
      return new Intl.NumberFormat("en-LK", {
        style: "currency",
        currency: "LKR",
        maximumFractionDigits: 2,
      }).format(value);
    }
  };

  const generatedAt = dashboard?.generatedAt
    ? new Intl.DateTimeFormat(settings.locale, {
        dateStyle: "medium",
        timeStyle: "short",
      }).format(new Date(dashboard.generatedAt))
    : null;

  const heroMetrics = dashboard?.heroMetrics ?? [];
  const alerts = dashboard?.alerts ?? [];
  const sections = dashboard?.sections ?? [];
  const quickActions = (dashboard?.quickActions?.length ? dashboard.quickActions : fallbackQuickActions).filter(
    (action, index, all) => all.findIndex((candidate) => candidate.href === action.href) === index,
  );

  return (
    <div className="space-y-6">
      <div className="grid gap-4 xl:grid-cols-[minmax(0,1.6fr)_minmax(18rem,0.9fr)]">
        <Card className="overflow-hidden border-transparent bg-[linear-gradient(135deg,color-mix(in_srgb,var(--accent)_14%,transparent),transparent_55%),radial-gradient(circle_at_top_right,color-mix(in_srgb,var(--accent)_18%,transparent),transparent_40%),var(--card-bg)]">
          <div className="text-xs font-semibold uppercase tracking-[0.28em] text-[var(--muted-foreground)]">
            Operational control tower
          </div>
          <h1 className="mt-3 text-3xl font-semibold tracking-tight text-[var(--foreground)]">Dashboard</h1>
          <p className="mt-3 max-w-3xl text-sm leading-7 text-[var(--muted-foreground)]">
            Role-aware queues, exception signals, and direct navigation into the work that needs attention next.
          </p>
          <div className="mt-6 flex flex-wrap gap-2">
            {quickActions.slice(0, 4).map((action) => (
              <SecondaryLink key={action.href} href={action.href}>
                {action.label}
              </SecondaryLink>
            ))}
          </div>
        </Card>

        <Card className="border-[var(--card-border)] bg-[var(--card-bg)]">
          <div className="text-sm font-semibold text-[var(--foreground)]">Snapshot</div>
          <div className="mt-4 space-y-4 text-sm">
            <div>
              <div className="text-[var(--muted-foreground)]">Last refresh</div>
              <div className="mt-1 font-medium text-[var(--foreground)]">
                {generatedAt ?? "Waiting for live data"}
              </div>
            </div>
            <div>
              <div className="text-[var(--muted-foreground)]">Attention alerts</div>
              <div className="mt-1 font-medium text-[var(--foreground)]">
                {alerts.length > 0 ? `${alerts.length} active` : "No active alerts"}
              </div>
            </div>
            <div>
              <div className="text-[var(--muted-foreground)]">Visible work queues</div>
              <div className="mt-1 font-medium text-[var(--foreground)]">{sections.length}</div>
            </div>
            <div>
              <div className="text-[var(--muted-foreground)]">Available actions</div>
              <div className="mt-1 font-medium text-[var(--foreground)]">{quickActions.length}</div>
            </div>
          </div>
        </Card>
      </div>

      {dashboardError ? (
        <Card className="border-amber-300 bg-amber-50/80 text-amber-900 dark:border-amber-900 dark:bg-amber-950/30 dark:text-amber-100">
          <div className="text-sm font-semibold">Dashboard data unavailable</div>
          <p className="mt-2 text-sm leading-6">{dashboardError}</p>
        </Card>
      ) : null}

      {heroMetrics.length > 0 ? (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-4">
          {heroMetrics.map((metric) => (
            <MetricCard key={`${metric.label}-${metric.href ?? "na"}`} metric={metric} formatMoney={formatMoney} />
          ))}
        </div>
      ) : (
        <Card>
          <div className="text-sm font-semibold text-[var(--foreground)]">No live metrics yet</div>
          <p className="mt-2 text-sm leading-6 text-[var(--muted-foreground)]">
            The dashboard will populate as operational transactions and balances start moving through the system.
          </p>
        </Card>
      )}

      <div className="space-y-3">
        <div>
          <h2 className="text-lg font-semibold text-[var(--foreground)]">Attention</h2>
          <p className="mt-1 text-sm text-[var(--muted-foreground)]">
            Exception-first signals that deserve review before they turn into service delays, stockouts, or cashflow drag.
          </p>
        </div>

        {alerts.length > 0 ? (
          <div className="grid gap-3 lg:grid-cols-2">
            {alerts.map((alert) => {
              const content = (
                <div
                  className={[
                    "rounded-2xl border px-4 py-4 transition hover:-translate-y-0.5",
                    alertToneClass(alert.severity),
                  ].join(" ")}
                >
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <div className="text-sm font-semibold">{alert.title}</div>
                      <p className="mt-1 text-sm leading-6">{alert.description}</p>
                    </div>
                    <div className="rounded-full border border-current/20 px-3 py-1 text-sm font-semibold">
                      {alert.count}
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
        ) : (
          <Card>
            <div className="text-sm font-semibold text-[var(--foreground)]">No urgent exceptions right now</div>
            <p className="mt-2 text-sm leading-6 text-[var(--muted-foreground)]">
              Use the work queues below to stay ahead of drafts, approvals, and settlement steps before they become exceptions.
            </p>
          </Card>
        )}
      </div>

      {sections.length > 0 ? (
        <div className="grid gap-4 2xl:grid-cols-2">
          {sections.map((section) => (
            <Card key={section.key} className="h-full border-[var(--card-border)] bg-[var(--card-bg)]">
              <div>
                <div className="text-lg font-semibold text-[var(--foreground)]">{section.title}</div>
                <p className="mt-2 text-sm leading-6 text-[var(--muted-foreground)]">{section.description}</p>
              </div>
              <div className="mt-5 space-y-3">
                {section.metrics.map((metric) => (
                  <SectionMetricRow
                    key={`${section.key}-${metric.label}-${metric.href ?? "na"}`}
                    metric={metric}
                    formatMoney={formatMoney}
                  />
                ))}
              </div>
            </Card>
          ))}
        </div>
      ) : null}

      {quickActions.length > 0 ? (
        <div className="space-y-3">
          <div>
            <h2 className="text-lg font-semibold text-[var(--foreground)]">Quick Access</h2>
            <p className="mt-1 text-sm text-[var(--muted-foreground)]">
              Jump straight into the modules you can act on right now.
            </p>
          </div>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {quickActions.map((action) => (
              <QuickActionCard key={action.href} action={action} />
            ))}
          </div>
        </div>
      ) : null}
    </div>
  );
}
