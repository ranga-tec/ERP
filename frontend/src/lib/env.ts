export const ISS_TOKEN_COOKIE = "iss_token";

export function issApiBaseUrl(): string {
  return process.env.ISS_API_BASE_URL ?? "http://localhost:5257";
}

