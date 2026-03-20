"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { Button, Card, Select } from "@/components/ui";
import { AssistantSettingsCard } from "./AssistantSettingsCard";
import {
  DEFAULT_USER_SETTINGS,
  applyThemePreference,
  loadUserSettingsClient,
  normalizeUserSettings,
  saveUserSettingsClient,
  type UserSettings,
} from "@/lib/user-settings";

type CurrencyOption = {
  id: string;
  code: string;
  name: string;
  isBase: boolean;
  isActive: boolean;
};

type WarehouseOption = {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
};

type TaxOption = {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
};

type PaymentTypeOption = {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
};

type SettingsPanelProps = {
  currencies: CurrencyOption[];
  warehouses: WarehouseOption[];
  taxes: TaxOption[];
  paymentTypes: PaymentTypeOption[];
};

function pickCurrencyCode(
  requestedCode: string,
  currencies: CurrencyOption[],
): string {
  const active = currencies.filter((x) => x.isActive);
  if (active.some((x) => x.code === requestedCode)) return requestedCode;

  const sriLankaCurrency = active.find((x) => x.code === "LKR");
  if (sriLankaCurrency) return sriLankaCurrency.code;

  const baseCurrency = active.find((x) => x.isBase);
  if (baseCurrency) return baseCurrency.code;

  return active[0]?.code ?? DEFAULT_USER_SETTINGS.baseCurrencyCode;
}

function pickReferenceId(
  requestedId: string | null,
  options: Array<{ id: string; isActive: boolean }>,
): string | null {
  const active = options.filter((x) => x.isActive);
  if (!requestedId) return null;
  return active.some((x) => x.id === requestedId) ? requestedId : null;
}

function pickSriLankaTaxCodeId(taxes: TaxOption[]): string | null {
  const active = taxes.filter((x) => x.isActive);
  const vat = active.find((x) => /vat/i.test(x.code) || /vat/i.test(x.name));
  return vat?.id ?? active[0]?.id ?? null;
}

function pickSriLankaPaymentTypeId(paymentTypes: PaymentTypeOption[]): string | null {
  const active = paymentTypes.filter((x) => x.isActive);
  const cash = active.find((x) => /cash/i.test(x.code) || /cash/i.test(x.name));
  return cash?.id ?? active[0]?.id ?? null;
}

function resolveSettingsAgainstReferences(
  settings: UserSettings,
  references: SettingsPanelProps,
): UserSettings {
  return {
    ...settings,
    baseCurrencyCode: pickCurrencyCode(settings.baseCurrencyCode, references.currencies),
    defaultWarehouseId: pickReferenceId(settings.defaultWarehouseId, references.warehouses),
    defaultTaxCodeId: pickReferenceId(settings.defaultTaxCodeId, references.taxes),
    defaultPaymentTypeId: pickReferenceId(settings.defaultPaymentTypeId, references.paymentTypes),
  };
}

function sriLankaDefaults(references: SettingsPanelProps): UserSettings {
  return resolveSettingsAgainstReferences(
    {
      ...DEFAULT_USER_SETTINGS,
      baseCurrencyCode: "LKR",
      locale: "en-LK",
      timeZone: "Asia/Colombo",
      defaultTaxCodeId: pickSriLankaTaxCodeId(references.taxes),
      defaultPaymentTypeId: pickSriLankaPaymentTypeId(references.paymentTypes),
      defaultWarehouseId: null,
    },
    references,
  );
}

export function SettingsPanel(props: SettingsPanelProps) {
  const refs = props;
  const [status, setStatus] = useState<string | null>(null);
  const [settings, setSettings] = useState<UserSettings>(() => {
    const loaded = normalizeUserSettings(loadUserSettingsClient());
    return resolveSettingsAgainstReferences(loaded, refs);
  });

  const activeCurrencies = useMemo(
    () => refs.currencies.filter((x) => x.isActive).sort((a, b) => a.code.localeCompare(b.code)),
    [refs.currencies],
  );

  const activeWarehouses = useMemo(
    () => refs.warehouses.filter((x) => x.isActive).sort((a, b) => a.code.localeCompare(b.code)),
    [refs.warehouses],
  );

  const activeTaxes = useMemo(
    () => refs.taxes.filter((x) => x.isActive).sort((a, b) => a.code.localeCompare(b.code)),
    [refs.taxes],
  );

  const activePaymentTypes = useMemo(
    () => refs.paymentTypes.filter((x) => x.isActive).sort((a, b) => a.code.localeCompare(b.code)),
    [refs.paymentTypes],
  );

  function save() {
    const normalized = normalizeUserSettings(settings);
    const resolved = resolveSettingsAgainstReferences(normalized, refs);
    const persisted = saveUserSettingsClient(resolved);
    setSettings(persisted);
    applyThemePreference(persisted.theme);
    setStatus("Saved.");
  }

  function resetToSriLanka() {
    const defaults = sriLankaDefaults(refs);
    setSettings(defaults);
    const persisted = saveUserSettingsClient(defaults);
    applyThemePreference(persisted.theme);
    setStatus("Reset to Sri Lanka defaults.");
  }

  return (
    <div className="space-y-6">
      <Card>
        <div className="mb-3 text-sm font-semibold">Theme and Localization</div>
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <label className="mb-1 block text-sm font-medium">Theme</label>
            <Select
              value={settings.theme}
              onChange={(e) => setSettings((s) => ({ ...s, theme: e.target.value as UserSettings["theme"] }))}
            >
              <option value="system">System</option>
              <option value="light">Light</option>
              <option value="dark">Dark</option>
            </Select>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">Locale</label>
            <Select
              value={settings.locale}
              onChange={(e) => setSettings((s) => ({ ...s, locale: e.target.value }))}
            >
              <option value="en-LK">English (Sri Lanka)</option>
              <option value="si-LK">Sinhala (Sri Lanka)</option>
              <option value="ta-LK">Tamil (Sri Lanka)</option>
              <option value="en-US">English (US)</option>
            </Select>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">Time Zone</label>
            <Select
              value={settings.timeZone}
              onChange={(e) => setSettings((s) => ({ ...s, timeZone: e.target.value }))}
            >
              <option value="Asia/Colombo">Asia/Colombo</option>
              <option value="UTC">UTC</option>
              <option value="Asia/Dubai">Asia/Dubai</option>
              <option value="Asia/Singapore">Asia/Singapore</option>
            </Select>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">Date Format</label>
            <Select
              value={settings.dateFormat}
              onChange={(e) => setSettings((s) => ({ ...s, dateFormat: e.target.value as UserSettings["dateFormat"] }))}
            >
              <option value="dd/MM/yyyy">dd/MM/yyyy</option>
              <option value="yyyy-MM-dd">yyyy-MM-dd</option>
              <option value="MM/dd/yyyy">MM/dd/yyyy</option>
            </Select>
          </div>
        </div>
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Default Transaction References</div>
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <label className="mb-1 block text-sm font-medium">Base Currency</label>
            <Select
              value={settings.baseCurrencyCode}
              onChange={(e) => setSettings((s) => ({ ...s, baseCurrencyCode: e.target.value }))}
            >
              {activeCurrencies.map((currency) => (
                <option key={currency.id} value={currency.code}>
                  {currency.code} - {currency.name}
                </option>
              ))}
            </Select>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">Default Warehouse</label>
            <Select
              value={settings.defaultWarehouseId ?? ""}
              onChange={(e) => setSettings((s) => ({ ...s, defaultWarehouseId: e.target.value || null }))}
            >
              <option value="">Not set</option>
              {activeWarehouses.map((warehouse) => (
                <option key={warehouse.id} value={warehouse.id}>
                  {warehouse.code} - {warehouse.name}
                </option>
              ))}
            </Select>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">Default Tax Code</label>
            <Select
              value={settings.defaultTaxCodeId ?? ""}
              onChange={(e) => setSettings((s) => ({ ...s, defaultTaxCodeId: e.target.value || null }))}
            >
              <option value="">Not set</option>
              {activeTaxes.map((tax) => (
                <option key={tax.id} value={tax.id}>
                  {tax.code} - {tax.name}
                </option>
              ))}
            </Select>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">Default Payment Type</label>
            <Select
              value={settings.defaultPaymentTypeId ?? ""}
              onChange={(e) => setSettings((s) => ({ ...s, defaultPaymentTypeId: e.target.value || null }))}
            >
              <option value="">Not set</option>
              {activePaymentTypes.map((paymentType) => (
                <option key={paymentType.id} value={paymentType.id}>
                  {paymentType.code} - {paymentType.name}
                </option>
              ))}
            </Select>
          </div>
        </div>

        <div className="mt-3 text-xs text-zinc-500">
          These defaults are linked to master data reference tables.
          {" "}
          <Link className="underline underline-offset-2" href="/master-data/currencies">Currencies</Link>
          {" | "}
          <Link className="underline underline-offset-2" href="/master-data/warehouses">Warehouses</Link>
          {" | "}
          <Link className="underline underline-offset-2" href="/master-data/taxes">Tax Codes</Link>
          {" | "}
          <Link className="underline underline-offset-2" href="/master-data/payment-types">Payment Types</Link>
        </div>
      </Card>

      <div className="flex flex-wrap gap-2">
        <Button type="button" onClick={save}>Save Settings</Button>
        <Button type="button" onClick={resetToSriLanka}>Reset to Sri Lanka Defaults</Button>
      </div>

      {status ? (
        <div className="text-sm text-zinc-600 dark:text-zinc-300">{status}</div>
      ) : null}

      <AssistantSettingsCard />
    </div>
  );
}
