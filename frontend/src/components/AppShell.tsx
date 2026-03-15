"use client";

import Link from "next/link";
import { useState, type ReactNode } from "react";
import { LogoutButton } from "@/components/LogoutButton";
import { Sidebar } from "@/components/Sidebar";

const SIDEBAR_COLLAPSED_STORAGE_KEY = "iss_sidebar_collapsed_v1";

type AppShellProps = {
  children: ReactNode;
  email: string;
  roles: string[];
};

function readCollapsedPreference(): boolean {
  if (typeof window === "undefined") return false;
  return window.localStorage.getItem(SIDEBAR_COLLAPSED_STORAGE_KEY) === "1";
}

export function AppShell({ children, email, roles }: AppShellProps) {
  const [sidebarCollapsed, setSidebarCollapsed] = useState<boolean>(() => readCollapsedPreference());
  const [mobileSidebarOpen, setMobileSidebarOpen] = useState(false);

  function toggleDesktopSidebar() {
    setSidebarCollapsed((prev) => {
      const next = !prev;
      if (typeof window !== "undefined") {
        window.localStorage.setItem(SIDEBAR_COLLAPSED_STORAGE_KEY, next ? "1" : "0");
      }
      return next;
    });
  }

  return (
    <div className="relative h-screen overflow-hidden bg-[var(--background)] text-[var(--foreground)]">
      {mobileSidebarOpen ? (
        <button
          type="button"
          className="fixed inset-0 z-30 bg-slate-950/45 backdrop-blur-sm lg:hidden"
          aria-label="Close menu"
          onClick={() => setMobileSidebarOpen(false)}
        />
      ) : null}

      <div
        className="fixed inset-y-0 left-0 z-40 w-[18.5rem] max-w-[86vw] -translate-x-full transition-transform duration-200 lg:hidden data-[open=true]:translate-x-0"
        data-open={mobileSidebarOpen}
      >
        <Sidebar onNavigate={() => setMobileSidebarOpen(false)} />
      </div>

      <div className="relative flex h-full min-h-0">
        <div className="hidden h-full lg:block">
          <Sidebar
            collapsed={sidebarCollapsed}
            onToggleCollapse={toggleDesktopSidebar}
          />
        </div>

        <div className="flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden">
          <header className="z-20 shrink-0 flex items-center justify-between border-b border-[var(--card-border)] bg-[var(--surface-soft)] px-4 py-4 shadow-[var(--shadow-soft)] backdrop-blur-xl sm:px-6">
            <div className="flex min-w-0 items-center gap-2">
              <button
                type="button"
                className="inline-flex items-center justify-center rounded-xl border border-[var(--input-border)] bg-[var(--surface)] px-3 py-1.5 text-sm font-medium text-[var(--foreground)] shadow-[var(--shadow-control)] transition-all duration-200 hover:-translate-y-px hover:bg-[var(--surface-soft)] hover:shadow-[var(--shadow-soft)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)] lg:hidden"
                onClick={() => setMobileSidebarOpen(true)}
              >
                Menu
              </button>
              <div className="min-w-0">
                <div className="truncate text-sm font-semibold">{email}</div>
                <div className="truncate text-xs text-[var(--muted-foreground)]">
                  {roles.length > 0 ? roles.join(", ") : "-"}
                </div>
              </div>
            </div>

            <div className="ml-3 flex items-center gap-2">
              <Link
                href="/settings"
                className="inline-flex items-center justify-center rounded-xl border border-[var(--input-border)] bg-[var(--surface)] px-3.5 py-2 text-sm font-medium text-[var(--foreground)] shadow-[var(--shadow-control)] transition-all duration-200 hover:-translate-y-px hover:bg-[var(--surface-soft)] hover:shadow-[var(--shadow-soft)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)]"
              >
                Settings
              </Link>
              <LogoutButton />
            </div>
          </header>

          <main className="min-h-0 flex-1 overflow-y-auto overscroll-contain animate-[pageFade_.28s_ease-out] p-4 sm:p-6 lg:p-8">
            {children}
          </main>
        </div>
      </div>
    </div>
  );
}
