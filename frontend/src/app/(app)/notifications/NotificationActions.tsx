"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiPostNoContent } from "@/lib/api-client";
import { SecondaryButton } from "@/components/ui";

export function NotificationMarkReadButton({ id }: { id: string }) {
  const router = useRouter();
  const [busy, setBusy] = useState(false);

  async function markRead() {
    setBusy(true);
    try {
      await apiPostNoContent(`notifications/${id}/read`, {});
      router.refresh();
    } finally {
      setBusy(false);
    }
  }

  return (
    <SecondaryButton type="button" disabled={busy} onClick={markRead}>
      {busy ? "Marking..." : "Mark read"}
    </SecondaryButton>
  );
}

export function NotificationMarkAllReadButton() {
  const router = useRouter();
  const [busy, setBusy] = useState(false);

  async function markAllRead() {
    setBusy(true);
    try {
      await apiPostNoContent("notifications/mark-all-read", {});
      router.refresh();
    } finally {
      setBusy(false);
    }
  }

  return (
    <SecondaryButton type="button" disabled={busy} onClick={markAllRead}>
      {busy ? "Updating..." : "Mark all read"}
    </SecondaryButton>
  );
}
