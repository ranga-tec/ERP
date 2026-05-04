"use client";

import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

const ALL_ROLES = [
  "Admin",
  "Procurement",
  "Inventory",
  "Sales",
  "Service",
  "Finance",
  "Reporting",
] as const;

type CompanyDto = { id: string; code: string; name: string; isActive: boolean };

export function UserCreateForm({ companies }: { companies: CompanyDto[] }) {
  const router = useRouter();
  const [companyId, setCompanyId] = useState(companies[0]?.id ?? "");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("Passw0rd1");
  const [displayName, setDisplayName] = useState("");
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const selectedRoles = useMemo(() => Array.from(selected.values()).sort(), [selected]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await apiPost("admin/users", {
        email,
        companyId: companyId || null,
        password,
        displayName: displayName || null,
        roles: selectedRoles,
      });
      setEmail("");
      setCompanyId(companies[0]?.id ?? "");
      setDisplayName("");
      setSelected(new Set());
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  function toggle(role: string) {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(role)) next.delete(role);
      else next.add(role);
      return next;
    });
  }

  return (
    <form onSubmit={submit} className="space-y-3">
      <div className="grid gap-3 md:grid-cols-4">
        <div>
          <label className="mb-1 block text-sm font-medium">Company</label>
          <Select value={companyId} onChange={(e) => setCompanyId(e.target.value)} required>
            {companies.map((company) => (
              <option key={company.id} value={company.id}>
                {company.code} - {company.name}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Email</label>
          <Input value={email} onChange={(e) => setEmail(e.target.value)} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Password</label>
          <Input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Display name</label>
          <Input value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
        </div>
      </div>

      <div>
        <div className="mb-1 text-sm font-medium">Roles</div>
        <div className="flex flex-wrap gap-3">
          {ALL_ROLES.map((role) => (
            <label key={role} className="flex items-center gap-2 text-sm">
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
        <div className="mt-1 text-xs text-zinc-500">
          Leave empty for a user with no module access.
        </div>
      </div>

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create user"}
      </Button>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-2 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}
    </form>
  );
}
