export const ISS_TOKEN_COOKIE = "iss_token";

export function issApiBaseUrl(): string {
  return process.env.ISS_API_BASE_URL ?? "http://localhost:5257";
}

export function issSecureCookies(): boolean {
  const raw = process.env.ISS_SECURE_COOKIES?.trim().toLowerCase();
  if (raw === "true") {
    return true;
  }

  if (raw === "false") {
    return false;
  }

  return process.env.NODE_ENV === "production";
}
