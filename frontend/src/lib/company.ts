/**
 * Single source of truth for company branding in the UI.
 * The matching letterhead used on generated PDFs lives in
 * `backend/src/ISS.Infrastructure/Documents/DocumentBranding.cs`.
 */
export const company = {
  name: "C-COM Equipment (Pvt) Ltd",
  shortName: "C-COM",
  tagline: "Equipment sales, service & operations",
  addressLine: "64/10 Nawala Road, Nugegoda, Sri Lanka",
  phone: "(+94) 112812994",
  email: "info@c-com.lk",
  website: "www.c-com.lk",
  websiteUrl: "https://www.c-com.lk/",
} as const;

export const brandAssets = {
  logo: "/brand/c-com-logo.png",
  logoDark: "/brand/c-com-logo-dark.png",
  mark: "/brand/c-com-mark.png",
  markDark: "/brand/c-com-mark-dark.png",
  wordmark: "/brand/c-com-wordmark.png",
  wordmarkDark: "/brand/c-com-wordmark-dark.png",
} as const;
