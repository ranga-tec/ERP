"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { apiGet } from "@/lib/api-client";

type NotificationCountDto = { unreadCount: number };

export function NotificationBell() {
  const [unreadCount, setUnreadCount] = useState(0);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      try {
        const result = await apiGet<NotificationCountDto>("notifications/unread-count");
        if (!cancelled) {
          setUnreadCount(result.unreadCount);
        }
      } catch {
        if (!cancelled) {
          setUnreadCount(0);
        }
      }
    }

    void load();
    const handle = window.setInterval(load, 60_000);
    return () => {
      cancelled = true;
      window.clearInterval(handle);
    };
  }, []);

  return (
    <Link
      href="/notifications"
      className="relative inline-flex min-h-8 items-center justify-center rounded-md border border-[var(--input-border)] bg-[var(--surface-soft)] px-3 py-1.5 text-[13px] font-medium text-[var(--foreground)] shadow-[var(--shadow-control)] transition-colors duration-150 hover:bg-[var(--surface)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)]"
    >
      Notifications
      {unreadCount > 0 ? (
        <span className="ml-2 rounded-full bg-red-600 px-1.5 py-0.5 text-[11px] font-semibold text-white">
          {unreadCount > 99 ? "99+" : unreadCount}
        </span>
      ) : null}
    </Link>
  );
}
