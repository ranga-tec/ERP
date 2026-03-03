"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

export function LogoutButton() {
  const router = useRouter();
  const [busy, setBusy] = useState(false);

  async function onLogout() {
    setBusy(true);
    try {
      await fetch("/api/auth/logout", { method: "POST" });
    } finally {
      setBusy(false);
      router.replace("/login");
      router.refresh();
    }
  }

  return (
    <button
      type="button"
      onClick={onLogout}
      disabled={busy}
      className="inline-flex items-center justify-center rounded-xl border border-[var(--input-border)] bg-[var(--surface)] px-3.5 py-2 text-sm font-medium text-[var(--foreground)] shadow-[var(--shadow-control)] transition-all duration-200 hover:-translate-y-px hover:bg-[var(--surface-soft)] hover:shadow-[var(--shadow-soft)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)] disabled:cursor-not-allowed disabled:opacity-55"
    >
      {busy ? "Signing out..." : "Sign out"}
    </button>
  );
}
