"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { startTransition, useEffect, useRef, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { Button, SecondaryButton, Textarea } from "@/components/ui";

type AssistantMessage = {
  role: "user" | "assistant";
  content: string;
  occurredAt: string;
};

type AssistantStatus = {
  mode: string;
  title: string;
  summary: string;
};

type AssistantPurchaseOrderDraft = {
  id: string;
  number: string;
  status: string;
  supplierCode: string;
  supplierName: string;
  lineCount: number;
  total: number;
  path: string;
  createdFromRequisition: boolean;
};

type AssistantGoodsReceiptDraft = {
  id: string;
  number: string;
  status: string;
  purchaseOrderNumber: string;
  warehouseCode: string;
  warehouseName: string;
  lineCount: number;
  plannedQuantity: number;
  remainingLineCount: number;
  path: string;
};

type AssistantReportRequest = {
  kind: string;
  title: string;
  apiPath: string;
  openPath: string;
  summary?: string | null;
};

type AssistantChatResponse = {
  sessionId: string;
  messages: AssistantMessage[];
  status: AssistantStatus;
  purchaseOrderDraft: AssistantPurchaseOrderDraft | null;
  goodsReceiptDraft: AssistantGoodsReceiptDraft | null;
  reportRequest: AssistantReportRequest | null;
  navigateTo: string | null;
  refreshCurrentPage: boolean;
};

type ReportPreview = {
  title: string;
  openPath: string;
  description?: string;
  summary: Array<{ label: string; value: string }>;
  columns: string[];
  rows: Array<Record<string, string>>;
};

const ASSISTANT_OPEN_KEY = "iss_assistant_open_v1";

type AssistantSettingsSummary = {
  isAllowed: boolean;
  disabledReason?: string | null;
  preference: {
    assistantEnabled: boolean;
    activeProviderProfileId: string | null;
  };
  providers: Array<{
    id: string;
    name: string;
    kind: string;
    model: string;
    isActive: boolean;
  }>;
};

const DEFAULT_STATUS: AssistantStatus = {
  mode: "idle",
  title: "Assistant",
  summary: "Ready. Phase 1 supports purchase-order drafting, GRN drafting, and report previews.",
};

const DEFAULT_MESSAGES: AssistantMessage[] = [
  {
    role: "assistant",
    content:
      "I can create a PO draft, guide a GRN from a PO step by step, and preview dashboard, stock ledger, aging, or costing reports. Configure an AI provider in Settings if you want richer NLP handling.",
    occurredAt: "",
  },
];

function readPanelOpen(): boolean {
  if (typeof window === "undefined") return false;
  return window.localStorage.getItem(ASSISTANT_OPEN_KEY) === "1";
}

function stringifyValue(value: unknown): string {
  if (value == null) return "-";
  if (typeof value === "string") return value;
  if (typeof value === "number" || typeof value === "boolean") return String(value);
  if (Array.isArray(value)) return value.map((item) => stringifyValue(item)).join(", ");
  return JSON.stringify(value);
}

function isPlainObject(value: unknown): value is Record<string, unknown> {
  return !!value && typeof value === "object" && !Array.isArray(value);
}

function buildReportPreview(report: AssistantReportRequest, data: unknown): ReportPreview {
  if (isPlainObject(data) && Array.isArray(data.rows)) {
    const rows = data.rows.filter(isPlainObject);
    const columns = rows.length > 0 ? Object.keys(rows[0]).slice(0, 8) : [];
    const summary = Object.entries(data)
      .filter(([key]) => key !== "rows")
      .map(([label, value]) => ({ label, value: stringifyValue(value) }));
    return {
      title: report.title,
      openPath: report.openPath,
      description: report.summary ?? undefined,
      summary,
      columns,
      rows: rows.slice(0, 12).map((row) =>
        Object.fromEntries(columns.map((column) => [column, stringifyValue(row[column])])),
      ),
    };
  }

  if (Array.isArray(data) && data.every(isPlainObject)) {
    const rows = data as Record<string, unknown>[];
    const columns = rows.length > 0 ? Object.keys(rows[0]).slice(0, 8) : [];
    return {
      title: report.title,
      openPath: report.openPath,
      description: report.summary ?? undefined,
      summary: [{ label: "count", value: String(rows.length) }],
      columns,
      rows: rows.slice(0, 12).map((row) =>
        Object.fromEntries(columns.map((column) => [column, stringifyValue(row[column])])),
      ),
    };
  }

  if (isPlainObject(data)) {
    return {
      title: report.title,
      openPath: report.openPath,
      description: report.summary ?? undefined,
      summary: Object.entries(data).map(([label, value]) => ({ label, value: stringifyValue(value) })),
      columns: [],
      rows: [],
    };
  }

  return {
    title: report.title,
    openPath: report.openPath,
    description: report.summary ?? undefined,
    summary: [{ label: "result", value: stringifyValue(data) }],
    columns: [],
    rows: [],
  };
}

function timeLabel(iso: string): string {
  const parsed = new Date(iso);
  return Number.isNaN(parsed.getTime()) ? "" : parsed.toLocaleTimeString();
}

export function AssistantPanel() {
  const router = useRouter();
  const pathname = usePathname();
  const messagesEndRef = useRef<HTMLDivElement | null>(null);

  const [isOpen, setIsOpen] = useState<boolean>(() => readPanelOpen());
  const [assistantSettings, setAssistantSettings] = useState<AssistantSettingsSummary | null>(null);
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [messages, setMessages] = useState<AssistantMessage[]>(DEFAULT_MESSAGES);
  const [status, setStatus] = useState<AssistantStatus>(DEFAULT_STATUS);
  const [purchaseOrderDraft, setPurchaseOrderDraft] = useState<AssistantPurchaseOrderDraft | null>(null);
  const [goodsReceiptDraft, setGoodsReceiptDraft] = useState<AssistantGoodsReceiptDraft | null>(null);
  const [reportPreview, setReportPreview] = useState<ReportPreview | null>(null);
  const [input, setInput] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function loadAssistantSettings() {
    try {
      const response = await apiGet<AssistantSettingsSummary>("assistant/settings");
      setAssistantSettings(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  }

  useEffect(() => {
    window.localStorage.setItem(ASSISTANT_OPEN_KEY, isOpen ? "1" : "0");
  }, [isOpen]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth", block: "end" });
  }, [messages, purchaseOrderDraft, goodsReceiptDraft, reportPreview, busy]);

  useEffect(() => {
    let cancelled = false;

    void (async () => {
      try {
        const response = await apiGet<AssistantSettingsSummary>("assistant/settings");
        if (!cancelled) {
          setAssistantSettings(response);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : String(err));
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (!isOpen) return;
    void loadAssistantSettings();
  }, [isOpen]);

  async function loadReportPreview(request: AssistantReportRequest) {
    const reportData = await apiGet<unknown>(request.apiPath);
    setReportPreview(buildReportPreview(request, reportData));
  }

  async function sendMessage(rawMessage: string) {
    const message = rawMessage.trim();
    if (!message || busy) return;
    if (assistantSettings && !assistantSettings.isAllowed) {
      setError(assistantSettings.disabledReason ?? "AI mode is not available.");
      return;
    }

    setBusy(true);
    setError(null);
    try {
      const response = await apiPost<AssistantChatResponse>("assistant/chat", {
        sessionId,
        message,
        providerProfileId: assistantSettings?.preference.activeProviderProfileId ?? null,
        provider: null,
      });

      setSessionId(response.sessionId);
      setMessages(response.messages.length > 0 ? response.messages : DEFAULT_MESSAGES);
      setStatus(response.status);
      setPurchaseOrderDraft(response.purchaseOrderDraft);
      setGoodsReceiptDraft(response.goodsReceiptDraft);

      if (response.reportRequest) {
        await loadReportPreview(response.reportRequest);
      } else {
        setReportPreview(null);
      }

      const navigateTo = response.navigateTo;
      if (navigateTo) {
        startTransition(() => {
          if (navigateTo === pathname) {
            router.refresh();
          } else {
            router.push(navigateTo);
          }
        });
      } else if (response.refreshCurrentPage) {
        startTransition(() => {
          router.refresh();
        });
      }

      setInput("");
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  function resetConversation() {
    setSessionId(null);
    setMessages(DEFAULT_MESSAGES);
    setStatus(DEFAULT_STATUS);
    setPurchaseOrderDraft(null);
    setGoodsReceiptDraft(null);
    setReportPreview(null);
    setInput("");
    setError(null);
  }

  const activeProvider = assistantSettings?.providers.find((provider) => provider.isActive) ?? null;

  if (!isOpen) {
    return (
      <button
        type="button"
        className="fixed bottom-4 right-4 z-50 rounded-2xl border border-[var(--input-border)] bg-[var(--accent)] px-4 py-3 text-sm font-semibold text-[var(--accent-contrast)] shadow-[var(--shadow-button)] transition hover:-translate-y-px hover:bg-[var(--accent-hover)]"
        onClick={() => setIsOpen(true)}
      >
        Assistant
      </button>
    );
  }

  return (
    <div className="fixed bottom-3 right-3 z-50 flex h-[min(78vh,44rem)] w-[min(28rem,calc(100vw-1.5rem))] flex-col overflow-hidden rounded-[1.6rem] border border-[var(--card-border)] bg-[var(--card-bg)] shadow-[var(--shadow-card)] backdrop-blur-xl">
      <div className="border-b border-[var(--card-border)] bg-[var(--surface-soft)] px-4 py-3">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <div className="text-sm font-semibold">{status.title}</div>
            <div className="mt-1 text-xs text-[var(--muted-foreground)]">{status.summary}</div>
          </div>
          <div className="flex items-center gap-2">
            <SecondaryButton type="button" className="px-2.5 py-1.5 text-xs" onClick={() => router.push("/settings")}>
              Settings
            </SecondaryButton>
            <SecondaryButton type="button" className="px-2.5 py-1.5 text-xs" onClick={resetConversation}>
              New
            </SecondaryButton>
            <SecondaryButton type="button" className="px-2.5 py-1.5 text-xs" onClick={() => setIsOpen(false)}>
              Close
            </SecondaryButton>
          </div>
        </div>
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto px-4 py-4">
        <div className="space-y-3">
          {assistantSettings && !assistantSettings.isAllowed ? (
            <div className="rounded-2xl border border-[var(--card-border)] bg-[var(--surface-soft)] p-3 text-sm">
              <div className="font-semibold">AI Mode Unavailable</div>
              <div className="mt-2 text-[var(--muted-foreground)]">
                {assistantSettings.disabledReason ?? "AI mode is disabled for this account."}
              </div>
              <SecondaryButton type="button" className="mt-3 px-3 py-2 text-xs" onClick={() => router.push("/settings")}>
                Open Settings
              </SecondaryButton>
            </div>
          ) : null}

          {messages.map((message, index) => (
            <div
              key={`${message.role}-${message.occurredAt}-${index}`}
              className={[
                "max-w-[92%] rounded-2xl px-3 py-2 text-sm shadow-[var(--shadow-soft)]",
                message.role === "user"
                  ? "ml-auto bg-[var(--accent)] text-[var(--accent-contrast)]"
                  : "bg-[var(--surface)] text-[var(--foreground)]",
              ].join(" ")}
            >
              <div className="whitespace-pre-wrap">{message.content}</div>
              <div
                className={[
                  "mt-1 text-[10px]",
                  message.role === "user" ? "text-[var(--accent-contrast)]/70" : "text-[var(--muted-foreground)]",
                ].join(" ")}
              >
                {timeLabel(message.occurredAt)}
              </div>
            </div>
          ))}

          {purchaseOrderDraft ? (
            <div className="rounded-2xl border border-[var(--card-border)] bg-[var(--surface-soft)] p-3 text-sm">
              <div className="font-semibold">Purchase Order</div>
              <div className="mt-2 space-y-1 text-[var(--muted-foreground)]">
                <div>Number: <span className="font-medium text-[var(--foreground)]">{purchaseOrderDraft.number}</span></div>
                <div>Status: <span className="font-medium text-[var(--foreground)]">{purchaseOrderDraft.status}</span></div>
                <div>Supplier: <span className="font-medium text-[var(--foreground)]">{purchaseOrderDraft.supplierCode} - {purchaseOrderDraft.supplierName}</span></div>
                <div>Lines: <span className="font-medium text-[var(--foreground)]">{purchaseOrderDraft.lineCount}</span></div>
                <div>Total: <span className="font-medium text-[var(--foreground)]">{purchaseOrderDraft.total}</span></div>
                <div>Source: <span className="font-medium text-[var(--foreground)]">{purchaseOrderDraft.createdFromRequisition ? "Requisition" : "Fresh draft"}</span></div>
              </div>
              <Link href={purchaseOrderDraft.path} className="mt-3 inline-block text-xs font-semibold text-[var(--link)] underline underline-offset-2">
                Open PO
              </Link>
            </div>
          ) : null}

          {goodsReceiptDraft ? (
            <div className="rounded-2xl border border-[var(--card-border)] bg-[var(--surface-soft)] p-3 text-sm">
              <div className="font-semibold">Goods Receipt</div>
              <div className="mt-2 space-y-1 text-[var(--muted-foreground)]">
                <div>Number: <span className="font-medium text-[var(--foreground)]">{goodsReceiptDraft.number}</span></div>
                <div>Status: <span className="font-medium text-[var(--foreground)]">{goodsReceiptDraft.status}</span></div>
                <div>PO: <span className="font-medium text-[var(--foreground)]">{goodsReceiptDraft.purchaseOrderNumber}</span></div>
                <div>Warehouse: <span className="font-medium text-[var(--foreground)]">{goodsReceiptDraft.warehouseCode} - {goodsReceiptDraft.warehouseName}</span></div>
                <div>GRN lines: <span className="font-medium text-[var(--foreground)]">{goodsReceiptDraft.lineCount}</span></div>
                <div>Planned qty: <span className="font-medium text-[var(--foreground)]">{goodsReceiptDraft.plannedQuantity}</span></div>
                <div>Still open on PO: <span className="font-medium text-[var(--foreground)]">{goodsReceiptDraft.remainingLineCount}</span></div>
              </div>
              <Link href={goodsReceiptDraft.path} className="mt-3 inline-block text-xs font-semibold text-[var(--link)] underline underline-offset-2">
                Open GRN
              </Link>
            </div>
          ) : null}

          {reportPreview ? (
            <div className="rounded-2xl border border-[var(--card-border)] bg-[var(--surface-soft)] p-3 text-sm">
              <div className="flex items-center justify-between gap-2">
                <div className="font-semibold">{reportPreview.title}</div>
                <Link href={reportPreview.openPath} className="text-xs font-semibold text-[var(--link)] underline underline-offset-2">
                  Open
                </Link>
              </div>
              {reportPreview.description ? (
                <div className="mt-2 text-xs text-[var(--muted-foreground)]">{reportPreview.description}</div>
              ) : null}
              {reportPreview.summary.length > 0 ? (
                <div className="mt-3 grid gap-2 sm:grid-cols-2">
                  {reportPreview.summary.map((entry) => (
                    <div key={entry.label} className="rounded-xl border border-[var(--card-border)] bg-[var(--surface)] px-3 py-2">
                      <div className="text-[11px] uppercase tracking-wide text-[var(--muted-foreground)]">{entry.label}</div>
                      <div className="mt-1 text-sm font-medium">{entry.value}</div>
                    </div>
                  ))}
                </div>
              ) : null}
              {reportPreview.columns.length > 0 ? (
                <div className="mt-3 overflow-x-auto rounded-xl border border-[var(--card-border)]">
                  <table className="w-full border-separate border-spacing-0 text-xs">
                    <thead>
                      <tr className="bg-[var(--surface)] text-left">
                        {reportPreview.columns.map((column) => (
                          <th key={column} className="border-b border-[var(--card-border)] px-3 py-2 font-semibold">
                            {column}
                          </th>
                        ))}
                      </tr>
                    </thead>
                    <tbody>
                      {reportPreview.rows.map((row, rowIndex) => (
                        <tr key={`${reportPreview.title}-${rowIndex}`}>
                          {reportPreview.columns.map((column) => (
                            <td key={`${rowIndex}-${column}`} className="border-b border-[var(--card-border)] px-3 py-2 align-top">
                              {row[column]}
                            </td>
                          ))}
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : null}
            </div>
          ) : null}

          <div ref={messagesEndRef} />
        </div>
      </div>

      <form
        className="border-t border-[var(--card-border)] bg-[var(--surface-soft)] px-4 py-3"
        onSubmit={async (event) => {
          event.preventDefault();
          await sendMessage(input);
        }}
      >
        <div className="space-y-3">
          <Textarea
            value={input}
            onChange={(event) => setInput(event.target.value)}
            onKeyDown={async (event) => {
              if (event.key === "Enter" && !event.shiftKey) {
                event.preventDefault();
                await sendMessage(input);
              }
            }}
            className="min-h-20 resize-none"
            placeholder="Ask to create a PO, start a GRN, revise a draft, or show a report..."
          />
          {error ? <div className="text-xs text-red-700 dark:text-red-300">{error}</div> : null}
          <div className="flex items-center justify-between gap-3">
            <div className="text-xs text-[var(--muted-foreground)]">
              {busy
                ? "Working..."
                : activeProvider
                  ? `Provider: ${activeProvider.name} (${activeProvider.model})`
                  : "Deterministic fallback"}
            </div>
            <Button type="submit" disabled={busy || !input.trim() || (assistantSettings ? !assistantSettings.isAllowed : false)}>
              {busy ? "Sending..." : "Send"}
            </Button>
          </div>
        </div>
      </form>
    </div>
  );
}
