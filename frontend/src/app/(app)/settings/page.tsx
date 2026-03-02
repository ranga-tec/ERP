import { backendFetchJson } from "@/lib/backend.server";
import { SettingsPanel } from "./SettingsPanel";

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

async function safeFetch<T>(path: string, fallback: T): Promise<T> {
  try {
    return await backendFetchJson<T>(path);
  } catch {
    return fallback;
  }
}

export default async function SettingsPage() {
  const [currencies, warehouses, taxes, paymentTypes] = await Promise.all([
    safeFetch<CurrencyOption[]>("/currencies", []),
    safeFetch<WarehouseOption[]>("/warehouses", []),
    safeFetch<TaxOption[]>("/taxes", []),
    safeFetch<PaymentTypeOption[]>("/payment-types", []),
  ]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Settings</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Personal UI and default transaction preferences.
        </p>
      </div>

      <SettingsPanel
        currencies={currencies}
        warehouses={warehouses}
        taxes={taxes}
        paymentTypes={paymentTypes}
      />
    </div>
  );
}
