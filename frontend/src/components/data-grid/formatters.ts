const defaultNumberLocale = "en-US";

export function formatGridNumber(
  value: number,
  options?: Intl.NumberFormatOptions & { locale?: string },
): string {
  return new Intl.NumberFormat(options?.locale ?? defaultNumberLocale, {
    maximumFractionDigits: 4,
    ...options,
  }).format(value);
}

export function formatGridMoney(
  value: number,
  options?: { currency?: string; locale?: string; fractionDigits?: number },
): string {
  const fractionDigits = options?.fractionDigits ?? 2;
  if (options?.currency) {
    return new Intl.NumberFormat(options.locale ?? defaultNumberLocale, {
      style: "currency",
      currency: options.currency,
      minimumFractionDigits: fractionDigits,
      maximumFractionDigits: fractionDigits,
    }).format(value);
  }

  return new Intl.NumberFormat(options?.locale ?? defaultNumberLocale, {
    minimumFractionDigits: fractionDigits,
    maximumFractionDigits: fractionDigits,
  }).format(value);
}

export function formatGridPercent(
  value: number,
  options?: { locale?: string; fractionDigits?: number },
): string {
  return `${formatGridNumber(value, {
    locale: options?.locale,
    minimumFractionDigits: 0,
    maximumFractionDigits: options?.fractionDigits ?? 2,
  })}%`;
}

export function formatGridDateTime(
  value: Date | number | string,
  options?: Intl.DateTimeFormatOptions & { locale?: string },
): string {
  return new Intl.DateTimeFormat(options?.locale ?? defaultNumberLocale, {
    dateStyle: "medium",
    timeStyle: "short",
    ...options,
  }).format(new Date(value));
}
