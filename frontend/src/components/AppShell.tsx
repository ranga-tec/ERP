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
    <div className="min-h-screen bg-zinc-50 text-zinc-900 dark:bg-black dark:text-zinc-100">
      {mobileSidebarOpen ? (
        <button
          type="button"
          className="fixed inset-0 z-30 bg-black/40 lg:hidden"
          aria-label="Close menu"
          onClick={() => setMobileSidebarOpen(false)}
        />
      ) : null}

      <div className="fixed inset-y-0 left-0 z-40 -translate-x-full transition-transform duration-200 lg:hidden data-[open=true]:translate-x-0" data-open={mobileSidebarOpen}>
        <Sidebar onNavigate={() => setMobileSidebarOpen(false)} />
      </div>

      <div className="flex min-h-screen">
        <div className="hidden lg:block">
          <Sidebar
            collapsed={sidebarCollapsed}
            onToggleCollapse={toggleDesktopSidebar}
          />
        </div>

        <div className="flex min-w-0 flex-1 flex-col">
          <header className="flex items-center justify-between border-b border-zinc-200 bg-white px-4 py-4 sm:px-6 dark:border-zinc-800 dark:bg-zinc-950">
            <div className="flex min-w-0 items-center gap-2">
              <button
                type="button"
                className="inline-flex rounded-md border border-zinc-300 bg-white px-3 py-1.5 text-sm font-medium hover:bg-zinc-50 lg:hidden dark:border-zinc-700 dark:bg-zinc-900 dark:hover:bg-zinc-800"
                onClick={() => setMobileSidebarOpen(true)}
              >
                Menu
              </button>
              <div className="min-w-0">
                <div className="truncate text-sm font-medium">{email}</div>
                <div className="truncate text-xs text-zinc-500">
                  {roles.length > 0 ? roles.join(", ") : "-"}
                </div>
              </div>
            </div>

            <div className="ml-3 flex items-center gap-2">
              <Link
                href="/settings"
                className="inline-block rounded-md border border-zinc-300 bg-white px-3 py-1.5 text-sm font-medium text-zinc-900 hover:bg-zinc-50 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-100 dark:hover:bg-zinc-800"
              >
                Settings
              </Link>
              <LogoutButton />
            </div>
          </header>

          <main className="flex-1 p-4 sm:p-6">{children}</main>
        </div>
      </div>
    </div>
  );
}
