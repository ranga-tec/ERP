"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useMemo, useState } from "react";

type Mode = "login" | "register";

const selfRegistrationEnabled = (() => {
  const configured = process.env.NEXT_PUBLIC_ISS_ALLOW_SELF_REGISTRATION;
  if (configured === "true") return true;
  if (configured === "false") return false;
  return process.env.NODE_ENV !== "production";
})();

function LoginPageInner() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const nextUrl = useMemo(() => searchParams.get("next") ?? "/", [searchParams]);
  const [mode, setMode] = useState<Mode>("login");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);

    try {
      if (mode === "register" && !selfRegistrationEnabled) {
        throw new Error("Registration is disabled. Contact an administrator.");
      }

      const endpoint = mode === "login" ? "/api/auth/login" : "/api/auth/register";
      const payload =
        mode === "login"
          ? { email, password }
          : { email, password, displayName: displayName || undefined };

      const resp = await fetch(endpoint, {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify(payload),
      });

      if (!resp.ok) {
        const text = await resp.text();
        throw new Error(text || `${resp.status} ${resp.statusText}`);
      }

      router.replace(nextUrl);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden px-4 py-8">
      <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(920px_420px_at_50%_-20%,var(--orb-a),transparent_66%),radial-gradient(900px_440px_at_110%_120%,var(--orb-b),transparent_72%)]" />

      <div className="relative w-full max-w-md rounded-3xl border border-[var(--card-border)] bg-[var(--card-bg)] p-7 shadow-[var(--shadow-card)] backdrop-blur-xl sm:p-8">
        <div className="mb-6">
          <div className="mb-1 inline-flex rounded-full border border-[var(--input-border)] bg-[var(--accent-muted)] px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.12em] text-[var(--link)]">
            ISS ERP Portal
          </div>
          <h1 className="text-2xl font-semibold tracking-tight">
            {mode === "login" ? "Sign in" : "Create account"}
          </h1>
          <p className="mt-1 text-sm text-[var(--muted-foreground)]">
            {mode === "login"
              ? "Use your ISS ERP credentials."
              : "The first registered user becomes Admin."}
          </p>
        </div>

        <form onSubmit={onSubmit} className="space-y-4">
          {mode === "register" ? (
            <div>
              <label className="mb-1 block text-sm font-medium">Display name</label>
              <input
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                className="w-full rounded-xl border border-[var(--input-border)] bg-[var(--surface)] px-3 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)] placeholder:text-[var(--muted-foreground)]/80"
                placeholder="Admin"
              />
            </div>
          ) : null}

          <div>
            <label className="mb-1 block text-sm font-medium">Email</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              className="w-full rounded-xl border border-[var(--input-border)] bg-[var(--surface)] px-3 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)] placeholder:text-[var(--muted-foreground)]/80"
              placeholder="admin@company.lk"
            />
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">Password</label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              className="w-full rounded-xl border border-[var(--input-border)] bg-[var(--surface)] px-3 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)] placeholder:text-[var(--muted-foreground)]/80"
              placeholder="Passw0rd1"
            />
          </div>

          {error ? (
            <div className="rounded-xl border border-[var(--danger)]/35 bg-[var(--danger-muted)] p-3 text-sm text-[var(--danger)]">
              {error}
            </div>
          ) : null}

          <button
            type="submit"
            disabled={busy}
            className="inline-flex w-full items-center justify-center rounded-xl bg-[var(--accent)] px-3.5 py-2.5 text-sm font-semibold text-[var(--accent-contrast)] shadow-[var(--shadow-button)] transition-all duration-200 hover:-translate-y-px hover:bg-[var(--accent-hover)] hover:shadow-[var(--shadow-button)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)] disabled:cursor-not-allowed disabled:opacity-55"
          >
            {busy
              ? mode === "login"
                ? "Signing in..."
                : "Creating..."
              : mode === "login"
                ? "Sign in"
                : "Create account"}
          </button>
        </form>

        <div className="mt-4 text-center text-sm text-[var(--muted-foreground)]">
          {mode === "login" && selfRegistrationEnabled ? (
            <button
              type="button"
              className="font-semibold text-[var(--link)] underline underline-offset-2 transition-colors hover:text-[var(--link-hover)]"
              onClick={() => setMode("register")}
            >
              Create an account
            </button>
          ) : mode === "register" ? (
            <button
              type="button"
              className="font-semibold text-[var(--link)] underline underline-offset-2 transition-colors hover:text-[var(--link-hover)]"
              onClick={() => setMode("login")}
            >
              Back to sign in
            </button>
          ) : (
            <span>Account registration is disabled.</span>
          )}
        </div>
      </div>
    </div>
  );
}

export default function LoginPage() {
  return (
    <Suspense
      fallback={
        <div className="flex min-h-screen items-center justify-center px-4">
          <div className="w-full max-w-md rounded-3xl border border-[var(--card-border)] bg-[var(--card-bg)] p-6 shadow-[var(--shadow-card)] backdrop-blur-xl">
            <div className="text-sm text-[var(--muted-foreground)]">Loading...</div>
          </div>
        </div>
      }
    >
      <LoginPageInner />
    </Suspense>
  );
}
