"use client";

import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import { apiPostNoContent, apiPut } from "@/lib/api-client";
import { Input, SecondaryButton } from "@/components/ui";

const ALL_ROLES = [
  "Admin",
  "Procurement",
  "Inventory",
  "Sales",
  "Service",
  "Finance",
  "Reporting",
] as const;

export function UserRowActions({
  userId,
  initialRoles,
  isLocked,
}: {
  userId: string;
  initialRoles: string[];
  isLocked: boolean;
}) {
  const router = useRouter();
  const [showRoles, setShowRoles] = useState(false);
  const [showReset, setShowReset] = useState(false);
  const [selected, setSelected] = useState<Set<string>>(new Set(initialRoles));
  const [newPassword, setNewPassword] = useState("Passw0rd2");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const roles = useMemo(() => Array.from(selected.values()).sort(), [selected]);

  function toggle(role: string) {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(role)) next.delete(role);
      else next.add(role);
      return next;
    });
  }

  async function saveRoles() {
    setError(null);
    setBusy(true);
    try {
      await apiPut(`admin/users/${userId}/roles`, { roles });
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  async function resetPassword() {
    setError(null);
    setBusy(true);
    try {
      await apiPostNoContent(`admin/users/${userId}/reset-password`, {
        newPassword,
      });
      setShowReset(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  async function toggleLock() {
    setError(null);
    setBusy(true);
    try {
      await apiPostNoContent(`admin/users/${userId}/${isLocked ? "enable" : "disable"}`, {});
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap gap-2">
        <SecondaryButton type="button" disabled={busy} onClick={() => setShowRoles((v) => !v)}>
          {showRoles ? "Hide roles" : "Edit roles"}
        </SecondaryButton>
        <SecondaryButton type="button" disabled={busy} onClick={() => setShowReset((v) => !v)}>
          {showReset ? "Hide reset" : "Reset password"}
        </SecondaryButton>
        <SecondaryButton type="button" disabled={busy} onClick={toggleLock}>
          {isLocked ? "Enable" : "Disable"}
        </SecondaryButton>
      </div>

      {showRoles ? (
        <div className="rounded-md border border-zinc-200 bg-zinc-50 p-2 text-sm dark:border-zinc-800 dark:bg-zinc-900">
          <div className="flex flex-wrap gap-3">
            {ALL_ROLES.map((role) => (
              <label key={role} className="flex items-center gap-2">
                <input
                  type="checkbox"
                  className="h-4 w-4"
                  checked={selected.has(role)}
                  onChange={() => toggle(role)}
                />
                {role}
              </label>
            ))}
          </div>
          <div className="mt-2">
            <SecondaryButton type="button" disabled={busy} onClick={saveRoles}>
              {busy ? "Saving..." : "Save roles"}
            </SecondaryButton>
          </div>
        </div>
      ) : null}

      {showReset ? (
        <div className="rounded-md border border-zinc-200 bg-zinc-50 p-2 text-sm dark:border-zinc-800 dark:bg-zinc-900">
          <div className="grid gap-2 sm:grid-cols-2">
            <Input
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
            />
            <SecondaryButton type="button" disabled={busy} onClick={resetPassword}>
              {busy ? "Resetting..." : "Reset"}
            </SecondaryButton>
          </div>
          <div className="mt-1 text-xs text-zinc-500">
            Password policy: min 8 chars + 1 digit.
          </div>
        </div>
      ) : null}

      {error ? <div className="text-xs text-red-600 dark:text-red-400">{error}</div> : null}
    </div>
  );
}

