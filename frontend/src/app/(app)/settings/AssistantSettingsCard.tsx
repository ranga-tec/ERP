"use client";

import { useEffect, useState } from "react";
import { apiDeleteNoContent, apiGet, apiPost, apiPut } from "@/lib/api-client";
import { Button, Card, Input, SecondaryButton, Select } from "@/components/ui";

type AssistantPolicy = {
  isEnabled: boolean;
  allowUserManagedProviders: boolean;
  allowedRoles: string[];
};

type AssistantPreference = {
  assistantEnabled: boolean;
  activeProviderProfileId: string | null;
};

type AssistantProviderProfile = {
  id: string;
  name: string;
  kind: string;
  baseUrl: string;
  model: string;
  hasApiKey: boolean;
  isActive: boolean;
  updatedAt: string;
};

type AssistantSettings = {
  canManagePolicy: boolean;
  canManageProviders: boolean;
  isAllowed: boolean;
  disabledReason?: string | null;
  policy: AssistantPolicy;
  preference: AssistantPreference;
  providers: AssistantProviderProfile[];
  availableRoles: string[];
  userRoles: string[];
};

type AssistantProviderDraft = {
  name: string;
  kind: string;
  baseUrl: string;
  model: string;
  apiKey: string;
  activateAfterSave: boolean;
};

type AssistantModelOption = {
  id: string;
  label: string;
};

type AssistantConnectionTest = {
  success: boolean;
  message: string;
};

const PROVIDER_OPTIONS = [
  { value: "openai", label: "OpenAI", baseUrl: "https://api.openai.com/v1" },
  { value: "anthropic", label: "Anthropic / Claude", baseUrl: "https://api.anthropic.com" },
  { value: "ollama", label: "Ollama", baseUrl: "http://localhost:11434" },
  { value: "openai-compatible", label: "OpenAI-Compatible", baseUrl: "https://api.openai.com/v1" },
];

function defaultDraft(kind = "openai"): AssistantProviderDraft {
  const provider = PROVIDER_OPTIONS.find((option) => option.value === kind) ?? PROVIDER_OPTIONS[0];
  return {
    name: "",
    kind: provider.value,
    baseUrl: provider.baseUrl,
    model: "",
    apiKey: "",
    activateAfterSave: true,
  };
}

function formatProviderKind(kind: string): string {
  return PROVIDER_OPTIONS.find((option) => option.value === kind)?.label ?? kind;
}

export function AssistantSettingsCard() {
  const [settings, setSettings] = useState<AssistantSettings | null>(null);
  const [policyDraft, setPolicyDraft] = useState<AssistantPolicy | null>(null);
  const [preferenceDraft, setPreferenceDraft] = useState<AssistantPreference | null>(null);
  const [providerDraft, setProviderDraft] = useState<AssistantProviderDraft>(() => defaultDraft());
  const [editingProviderId, setEditingProviderId] = useState<string | null>(null);
  const [discoveredModels, setDiscoveredModels] = useState<AssistantModelOption[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [status, setStatus] = useState<string | null>(null);
  const [testStatus, setTestStatus] = useState<AssistantConnectionTest | null>(null);

  async function loadSettings() {
    const response = await apiGet<AssistantSettings>("assistant/settings");
    setSettings(response);
    setPolicyDraft(response.policy);
    setPreferenceDraft(response.preference);
  }

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setLoading(true);
      try {
        const response = await apiGet<AssistantSettings>("assistant/settings");
        if (cancelled) return;
        setSettings(response);
        setPolicyDraft(response.policy);
        setPreferenceDraft(response.preference);
      } catch (error) {
        if (cancelled) return;
        setStatus(error instanceof Error ? error.message : String(error));
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    void load();
    return () => {
      cancelled = true;
    };
  }, []);

  async function savePreference() {
    if (!preferenceDraft) return;
    setSaving(true);
    setStatus(null);
    try {
      const saved = await apiPut<AssistantPreference>("assistant/settings/preference", preferenceDraft);
      setPreferenceDraft(saved);
      await loadSettings();
      setStatus("AI preferences saved.");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : String(error));
    } finally {
      setSaving(false);
    }
  }

  async function savePolicy() {
    if (!policyDraft) return;
    setSaving(true);
    setStatus(null);
    try {
      const saved = await apiPut<AssistantPolicy>("assistant/settings/policy", policyDraft);
      setPolicyDraft(saved);
      await loadSettings();
      setStatus("AI access policy saved.");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : String(error));
    } finally {
      setSaving(false);
    }
  }

  async function saveProvider() {
    setSaving(true);
    setStatus(null);
    try {
      const path = editingProviderId
        ? `assistant/settings/providers/${editingProviderId}`
        : "assistant/settings/providers";

      if (editingProviderId) {
        await apiPut<AssistantProviderProfile>(path, providerDraft);
      } else {
        await apiPost<AssistantProviderProfile>(path, providerDraft);
      }

      setEditingProviderId(null);
      setProviderDraft(defaultDraft(providerDraft.kind));
      setDiscoveredModels([]);
      await loadSettings();
      setStatus("AI provider profile saved.");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : String(error));
    } finally {
      setSaving(false);
    }
  }

  async function deleteProvider(providerId: string) {
    setSaving(true);
    setStatus(null);
    try {
      await apiDeleteNoContent(`assistant/settings/providers/${providerId}`);
      if (editingProviderId === providerId) {
        setEditingProviderId(null);
        setProviderDraft(defaultDraft());
        setDiscoveredModels([]);
      }
      await loadSettings();
      setStatus("AI provider profile deleted.");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : String(error));
    } finally {
      setSaving(false);
    }
  }

  async function testProvider() {
    setSaving(true);
    setTestStatus(null);
    try {
      const result = await apiPost<AssistantConnectionTest>("assistant/settings/providers/test", providerDraft);
      setTestStatus(result);
    } catch (error) {
      setTestStatus({ success: false, message: error instanceof Error ? error.message : String(error) });
    } finally {
      setSaving(false);
    }
  }

  async function discoverModels() {
    setSaving(true);
    setStatus(null);
    try {
      const models = await apiPost<AssistantModelOption[]>("assistant/settings/providers/models", providerDraft);
      setDiscoveredModels(models);
      setStatus(models.length > 0 ? `Discovered ${models.length} model(s).` : "No models were returned by the provider.");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : String(error));
      setDiscoveredModels([]);
    } finally {
      setSaving(false);
    }
  }

  function startEditing(provider: AssistantProviderProfile) {
    setEditingProviderId(provider.id);
    setProviderDraft({
      name: provider.name,
      kind: provider.kind,
      baseUrl: provider.baseUrl,
      model: provider.model,
      apiKey: "",
      activateAfterSave: provider.isActive,
    });
    setDiscoveredModels([]);
    setTestStatus(null);
    setStatus(`Editing ${provider.name}. Leave API key blank to keep the stored key.`);
  }

  function resetProviderForm() {
    setEditingProviderId(null);
    setProviderDraft(defaultDraft());
    setDiscoveredModels([]);
    setTestStatus(null);
    setStatus(null);
  }

  function changeProviderKind(kind: string) {
    const template = defaultDraft(kind);
    setProviderDraft((current) => ({
      ...current,
      kind,
      baseUrl: current.baseUrl.trim() === "" || current.baseUrl === defaultDraft(current.kind).baseUrl
        ? template.baseUrl
        : current.baseUrl,
    }));
    setDiscoveredModels([]);
    setTestStatus(null);
  }

  if (loading && !settings) {
    return (
      <Card>
        <div className="text-sm font-semibold">AI Assistant</div>
        <div className="mt-2 text-sm text-zinc-500">Loading AI settings...</div>
      </Card>
    );
  }

  if (!settings || !preferenceDraft || !policyDraft) {
    return (
      <Card>
        <div className="text-sm font-semibold">AI Assistant</div>
        <div className="mt-2 text-sm text-red-700 dark:text-red-300">
          {status ?? "Could not load AI settings."}
        </div>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <Card>
        <div className="mb-3 text-sm font-semibold">AI Assistant</div>
        <div className="grid gap-3 lg:grid-cols-2">
          <div className="rounded-xl border border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-3">
            <div className="text-xs uppercase tracking-wide text-[var(--muted-foreground)]">Access</div>
            <div className="mt-1 text-sm font-medium">
              {settings.isAllowed ? "Enabled for your account" : "Currently unavailable"}
            </div>
            <div className="mt-1 text-xs text-[var(--muted-foreground)]">
              {settings.disabledReason ?? "Your roles and personal settings currently allow AI access."}
            </div>
          </div>

          <div className="rounded-xl border border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-3">
            <div className="text-xs uppercase tracking-wide text-[var(--muted-foreground)]">Your Roles</div>
            <div className="mt-1 text-sm font-medium">{settings.userRoles.join(", ") || "-"}</div>
            <div className="mt-1 text-xs text-[var(--muted-foreground)]">
              Allowed roles: {settings.policy.allowedRoles.join(", ") || "None"}
            </div>
          </div>
        </div>

        <div className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          <label className="flex items-center gap-2 rounded-xl border border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-2 text-sm">
            <input
              type="checkbox"
              checked={preferenceDraft.assistantEnabled}
              onChange={(event) =>
                setPreferenceDraft((current) =>
                  current ? { ...current, assistantEnabled: event.target.checked } : current,
                )
              }
            />
            Enable AI mode for my account
          </label>

          <div>
            <label className="mb-1 block text-sm font-medium">Active Provider</label>
            <Select
              value={preferenceDraft.activeProviderProfileId ?? ""}
              onChange={(event) =>
                setPreferenceDraft((current) =>
                  current
                    ? {
                        ...current,
                        activeProviderProfileId: event.target.value || null,
                      }
                    : current,
                )
              }
            >
              <option value="">Deterministic fallback only</option>
              {settings.providers.map((provider) => (
                <option key={provider.id} value={provider.id}>
                  {provider.name} ({formatProviderKind(provider.kind)})
                </option>
              ))}
            </Select>
          </div>

          <div className="self-end">
            <Button type="button" onClick={savePreference} disabled={saving}>
              Save AI Settings
            </Button>
          </div>
        </div>

        <div className="mt-3 text-xs text-[var(--muted-foreground)]">
          If no provider is selected, the assistant still falls back to the built-in deterministic workflow logic.
        </div>
      </Card>

      {settings.canManagePolicy ? (
        <Card>
          <div className="mb-3 text-sm font-semibold">Admin AI Policy</div>
          <div className="grid gap-3 lg:grid-cols-2">
            <label className="flex items-center gap-2 rounded-xl border border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-2 text-sm">
              <input
                type="checkbox"
                checked={policyDraft.isEnabled}
                onChange={(event) =>
                  setPolicyDraft((current) =>
                    current ? { ...current, isEnabled: event.target.checked } : current,
                  )
                }
              />
              Enable AI mode system-wide
            </label>

            <label className="flex items-center gap-2 rounded-xl border border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-2 text-sm">
              <input
                type="checkbox"
                checked={policyDraft.allowUserManagedProviders}
                onChange={(event) =>
                  setPolicyDraft((current) =>
                    current ? { ...current, allowUserManagedProviders: event.target.checked } : current,
                  )
                }
              />
              Allow users to save their own provider profiles
            </label>
          </div>

          <div className="mt-4">
            <div className="mb-2 text-sm font-medium">Allowed Roles</div>
            <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-4">
              {settings.availableRoles.map((role) => {
                const checked = policyDraft.allowedRoles.includes(role);
                return (
                  <label
                    key={role}
                    className="flex items-center gap-2 rounded-xl border border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-2 text-sm"
                  >
                    <input
                      type="checkbox"
                      checked={checked}
                      onChange={(event) =>
                        setPolicyDraft((current) =>
                          current
                            ? {
                                ...current,
                                allowedRoles: event.target.checked
                                  ? [...current.allowedRoles, role].sort()
                                  : current.allowedRoles.filter((item) => item !== role),
                              }
                            : current,
                        )
                      }
                    />
                    {role}
                  </label>
                );
              })}
            </div>
          </div>

          <div className="mt-4">
            <Button type="button" onClick={savePolicy} disabled={saving}>
              Save Admin Policy
            </Button>
          </div>
        </Card>
      ) : null}

      <Card>
        <div className="mb-3 flex items-center justify-between gap-2">
          <div>
            <div className="text-sm font-semibold">Provider Profiles</div>
            <div className="mt-1 text-xs text-[var(--muted-foreground)]">
              Save provider configs on the server. API keys are encrypted before storage and redacted in audit logs.
            </div>
          </div>
          {settings.canManageProviders ? (
            <SecondaryButton type="button" onClick={resetProviderForm}>
              {editingProviderId ? "New Profile" : "Clear"}
            </SecondaryButton>
          ) : null}
        </div>

        {!settings.canManageProviders ? (
          <div className="rounded-xl border border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-3 text-sm text-[var(--muted-foreground)]">
            Personal AI provider management is currently disabled by policy.
          </div>
        ) : (
          <div className="space-y-4">
            <div className="grid gap-3 lg:grid-cols-2">
              <div>
                <label className="mb-1 block text-sm font-medium">Profile Name</label>
                <Input
                  value={providerDraft.name}
                  onChange={(event) => setProviderDraft((current) => ({ ...current, name: event.target.value }))}
                  placeholder="My OpenAI key"
                />
              </div>

              <div>
                <label className="mb-1 block text-sm font-medium">Provider</label>
                <Select value={providerDraft.kind} onChange={(event) => changeProviderKind(event.target.value)}>
                  {PROVIDER_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </Select>
              </div>

              <div>
                <label className="mb-1 block text-sm font-medium">Base URL</label>
                <Input
                  value={providerDraft.baseUrl}
                  onChange={(event) => setProviderDraft((current) => ({ ...current, baseUrl: event.target.value }))}
                  placeholder="https://api.openai.com/v1"
                />
              </div>

              <div>
                <label className="mb-1 block text-sm font-medium">Model</label>
                <Input
                  value={providerDraft.model}
                  onChange={(event) => setProviderDraft((current) => ({ ...current, model: event.target.value }))}
                  placeholder="gpt-5-mini / claude-sonnet / llama3.1"
                />
              </div>

              <div>
                <label className="mb-1 block text-sm font-medium">API Key</label>
                <Input
                  type="password"
                  value={providerDraft.apiKey}
                  onChange={(event) => setProviderDraft((current) => ({ ...current, apiKey: event.target.value }))}
                  placeholder={providerDraft.kind === "ollama" ? "Optional for local Ollama" : "Provider API key"}
                />
              </div>

              <label className="flex items-center gap-2 self-end rounded-xl border border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-2 text-sm">
                <input
                  type="checkbox"
                  checked={providerDraft.activateAfterSave}
                  onChange={(event) =>
                    setProviderDraft((current) => ({ ...current, activateAfterSave: event.target.checked }))
                  }
                />
                Make active after save
              </label>
            </div>

            {discoveredModels.length > 0 ? (
              <div>
                <label className="mb-1 block text-sm font-medium">Discovered Models</label>
                <Select
                  value=""
                  onChange={(event) => {
                    if (!event.target.value) return;
                    setProviderDraft((current) => ({ ...current, model: event.target.value }));
                  }}
                >
                  <option value="">Pick a discovered model</option>
                  {discoveredModels.map((model) => (
                    <option key={model.id} value={model.id}>
                      {model.label}
                    </option>
                  ))}
                </Select>
              </div>
            ) : null}

            <div className="flex flex-wrap gap-2">
              <SecondaryButton type="button" onClick={discoverModels} disabled={saving}>
                Discover Models
              </SecondaryButton>
              <SecondaryButton type="button" onClick={testProvider} disabled={saving}>
                Test Connection
              </SecondaryButton>
              <Button type="button" onClick={saveProvider} disabled={saving}>
                {editingProviderId ? "Update Profile" : "Save Profile"}
              </Button>
            </div>

            <div className="text-xs text-[var(--muted-foreground)]">
              Ollama uses the backend server to connect to the configured URL. If Ollama is on another machine, enter that reachable server address here.
            </div>

            {testStatus ? (
              <div className={["rounded-xl px-3 py-2 text-sm", testStatus.success ? "bg-emerald-100/80 text-emerald-900 dark:bg-emerald-500/15 dark:text-emerald-200" : "bg-red-100/80 text-red-900 dark:bg-red-500/15 dark:text-red-200"].join(" ")}>
                {testStatus.message}
              </div>
            ) : null}

            <div className="space-y-2">
              {settings.providers.map((provider) => (
                <div
                  key={provider.id}
                  className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-3"
                >
                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center gap-2 text-sm font-medium">
                      <span>{provider.name}</span>
                      {provider.isActive ? (
                        <span className="rounded-full bg-[var(--accent)] px-2 py-0.5 text-[11px] font-semibold text-[var(--accent-contrast)]">
                          Active
                        </span>
                      ) : null}
                    </div>
                    <div className="mt-1 text-xs text-[var(--muted-foreground)]">
                      {formatProviderKind(provider.kind)} | {provider.model} | {provider.baseUrl}
                    </div>
                    <div className="mt-1 text-xs text-[var(--muted-foreground)]">
                      API key: {provider.hasApiKey ? "Stored" : "Not stored"} | Updated: {new Date(provider.updatedAt).toLocaleString()}
                    </div>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    {!provider.isActive ? (
                      <SecondaryButton
                        type="button"
                        onClick={async () => {
                          setPreferenceDraft((current) =>
                            current ? { ...current, activeProviderProfileId: provider.id } : current,
                          );
                          try {
                            await apiPut<AssistantPreference>("assistant/settings/preference", {
                              assistantEnabled: preferenceDraft.assistantEnabled,
                              activeProviderProfileId: provider.id,
                            });
                            await loadSettings();
                            setStatus(`${provider.name} is now the active AI provider.`);
                          } catch (error) {
                            setStatus(error instanceof Error ? error.message : String(error));
                          }
                        }}
                      >
                        Activate
                      </SecondaryButton>
                    ) : null}
                    <SecondaryButton type="button" onClick={() => startEditing(provider)}>
                      Edit
                    </SecondaryButton>
                    <SecondaryButton type="button" onClick={() => void deleteProvider(provider.id)}>
                      Delete
                    </SecondaryButton>
                  </div>
                </div>
              ))}
              {settings.providers.length === 0 ? (
                <div className="rounded-xl border border-dashed border-[var(--card-border)] px-3 py-4 text-sm text-[var(--muted-foreground)]">
                  No AI provider profiles saved yet.
                </div>
              ) : null}
            </div>
          </div>
        )}
      </Card>

      {status ? <div className="text-sm text-zinc-600 dark:text-zinc-300">{status}</div> : null}
    </div>
  );
}
