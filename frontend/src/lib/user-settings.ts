export type ThemePreference = "system" | "light" | "dark";
export type DateFormatPreference = "dd/MM/yyyy" | "yyyy-MM-dd" | "MM/dd/yyyy";

export type UserSettings = {
  theme: ThemePreference;
  locale: string;
  timeZone: string;
  dateFormat: DateFormatPreference;
  baseCurrencyCode: string;
  defaultWarehouseId: string | null;
  defaultTaxCodeId: string | null;
  defaultPaymentTypeId: string | null;
};

export const USER_SETTINGS_STORAGE_KEY = "iss_user_settings_v1";
export const USER_SETTINGS_COOKIE = "iss_user_settings_v1";

export const DEFAULT_USER_SETTINGS: UserSettings = {
  theme: "system",
  locale: "en-LK",
  timeZone: "Asia/Colombo",
  dateFormat: "dd/MM/yyyy",
  baseCurrencyCode: "LKR",
  defaultWarehouseId: null,
  defaultTaxCodeId: null,
  defaultPaymentTypeId: null,
};

type PartialUserSettings = Partial<UserSettings> | null | undefined;

function normalizedTheme(value: unknown): ThemePreference {
  return value === "light" || value === "dark" || value === "system"
    ? value
    : DEFAULT_USER_SETTINGS.theme;
}

function normalizedDateFormat(value: unknown): DateFormatPreference {
  return value === "dd/MM/yyyy" || value === "yyyy-MM-dd" || value === "MM/dd/yyyy"
    ? value
    : DEFAULT_USER_SETTINGS.dateFormat;
}

function normalizedString(value: unknown, fallback: string): string {
  if (typeof value !== "string") return fallback;
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : fallback;
}

function normalizedId(value: unknown): string | null {
  if (typeof value !== "string") return null;
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
}

export function normalizeUserSettings(value: PartialUserSettings): UserSettings {
  return {
    theme: normalizedTheme(value?.theme),
    locale: normalizedString(value?.locale, DEFAULT_USER_SETTINGS.locale),
    timeZone: normalizedString(value?.timeZone, DEFAULT_USER_SETTINGS.timeZone),
    dateFormat: normalizedDateFormat(value?.dateFormat),
    baseCurrencyCode: normalizedString(
      typeof value?.baseCurrencyCode === "string"
        ? value.baseCurrencyCode.toUpperCase()
        : value?.baseCurrencyCode,
      DEFAULT_USER_SETTINGS.baseCurrencyCode,
    ),
    defaultWarehouseId: normalizedId(value?.defaultWarehouseId),
    defaultTaxCodeId: normalizedId(value?.defaultTaxCodeId),
    defaultPaymentTypeId: normalizedId(value?.defaultPaymentTypeId),
  };
}

function parseJsonSettings(raw: string): PartialUserSettings {
  try {
    return JSON.parse(raw) as Partial<UserSettings>;
  } catch {
    return null;
  }
}

function decodeCookieValue(value: string): string {
  try {
    return decodeURIComponent(value);
  } catch {
    return value;
  }
}

export function parseUserSettings(raw: string | null | undefined): UserSettings {
  if (!raw) return { ...DEFAULT_USER_SETTINGS };
  return normalizeUserSettings(parseJsonSettings(decodeCookieValue(raw)));
}

export function serializeUserSettings(settings: UserSettings): string {
  return JSON.stringify(normalizeUserSettings(settings));
}

function readSettingsCookieRaw(): string | null {
  if (typeof document === "undefined") return null;
  const parts = document.cookie.split(";").map((part) => part.trim());
  const prefix = `${USER_SETTINGS_COOKIE}=`;
  const match = parts.find((part) => part.startsWith(prefix));
  return match ? match.slice(prefix.length) : null;
}

export function loadUserSettingsClient(): UserSettings {
  if (typeof window === "undefined") {
    return { ...DEFAULT_USER_SETTINGS };
  }

  const fromStorage = window.localStorage.getItem(USER_SETTINGS_STORAGE_KEY);
  if (fromStorage) {
    return parseUserSettings(fromStorage);
  }

  const fromCookie = readSettingsCookieRaw();
  if (fromCookie) {
    return parseUserSettings(fromCookie);
  }

  return { ...DEFAULT_USER_SETTINGS };
}

export function saveUserSettingsClient(settings: UserSettings): UserSettings {
  const normalized = normalizeUserSettings(settings);
  const serialized = serializeUserSettings(normalized);

  if (typeof window !== "undefined") {
    window.localStorage.setItem(USER_SETTINGS_STORAGE_KEY, serialized);
  }

  if (typeof document !== "undefined") {
    const encoded = encodeURIComponent(serialized);
    const oneYear = 60 * 60 * 24 * 365;
    document.cookie = `${USER_SETTINGS_COOKIE}=${encoded}; path=/; max-age=${oneYear}; samesite=lax`;
  }

  return normalized;
}

function resolveEffectiveTheme(preference: ThemePreference): "light" | "dark" {
  if (preference === "dark") return "dark";
  if (preference === "light") return "light";
  if (typeof window === "undefined") return "light";
  return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

export function applyThemePreference(preference: ThemePreference): void {
  if (typeof document === "undefined") return;
  const effective = resolveEffectiveTheme(preference);
  const root = document.documentElement;
  root.classList.toggle("dark", effective === "dark");
  root.style.colorScheme = effective;
}

export function userSettingsThemeBootstrapScript(): string {
  const key = USER_SETTINGS_STORAGE_KEY;
  const cookieName = USER_SETTINGS_COOKIE;

  return `
  (function () {
    try {
      var raw = localStorage.getItem(${JSON.stringify(key)});
      if (!raw) {
        var parts = document.cookie.split(';');
        var prefix = ${JSON.stringify(`${cookieName}=`)};
        for (var i = 0; i < parts.length; i += 1) {
          var part = parts[i].trim();
          if (part.indexOf(prefix) === 0) {
            raw = decodeURIComponent(part.slice(prefix.length));
            break;
          }
        }
      }

      var theme = "system";
      if (raw) {
        var parsed = JSON.parse(raw);
        if (parsed && (parsed.theme === "light" || parsed.theme === "dark" || parsed.theme === "system")) {
          theme = parsed.theme;
        }
      }

      var dark = theme === "dark" || (theme === "system" && window.matchMedia("(prefers-color-scheme: dark)").matches);
      document.documentElement.classList.toggle("dark", dark);
      document.documentElement.style.colorScheme = dark ? "dark" : "light";
    } catch (_err) {
      document.documentElement.classList.toggle("dark", false);
      document.documentElement.style.colorScheme = "light";
    }
  })();
  `;
}
