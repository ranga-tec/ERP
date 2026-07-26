export const NEUEDGE_TOKEN_COOKIE = "neuedge_token";

export function neuedgeApiBaseUrl(): string {
  return process.env.NEUEDGE_API_BASE_URL ?? "http://localhost:5257";
}

export function neuedgeSecureCookies(): boolean {
  const raw = process.env.NEUEDGE_SECURE_COOKIES?.trim().toLowerCase();
  if (raw === "true") {
    return true;
  }

  if (raw === "false") {
    return false;
  }

  return process.env.NODE_ENV === "production";
}
