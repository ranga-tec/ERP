export type IssSession = {
  userId: string;
  email?: string;
  roles: string[];
};

function base64UrlDecode(input: string): string {
  const base64 = input.replace(/-/g, "+").replace(/_/g, "/");
  const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), "=");
  if (typeof atob === "function") {
    const binary = atob(padded);
    const bytes = Uint8Array.from(binary, (ch) => ch.charCodeAt(0));
    return new TextDecoder().decode(bytes);
  }

  return Buffer.from(padded, "base64").toString("utf8");
}

export function decodeJwtPayload(token: string): unknown {
  const parts = token.split(".");
  if (parts.length < 2) return null;
  try {
    return JSON.parse(base64UrlDecode(parts[1]));
  } catch {
    return null;
  }
}

export function sessionFromToken(token: string): IssSession | null {
  const payload = decodeJwtPayload(token);
  if (!payload || typeof payload !== "object") return null;

  const record = payload as Record<string, unknown>;
  const sub = typeof record.sub === "string" ? record.sub : undefined;
  const nameId =
    typeof record["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] ===
    "string"
      ? (record[
          "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
        ] as string)
      : undefined;

  const email =
    typeof record["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] ===
    "string"
      ? (record[
          "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
        ] as string)
      : undefined;

  const roleClaim =
    record["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
  const roles = Array.isArray(roleClaim)
    ? roleClaim.filter((r): r is string => typeof r === "string")
    : typeof roleClaim === "string"
      ? [roleClaim]
      : [];

  const userId = sub ?? nameId;
  if (!userId) return null;

  return { userId, email, roles };
}

export function jwtExpUnix(token: string): number | null {
  const payload = decodeJwtPayload(token);
  if (!payload || typeof payload !== "object") return null;

  const record = payload as Record<string, unknown>;
  const exp = record.exp;
  return typeof exp === "number" && Number.isFinite(exp) ? exp : null;
}

export function isJwtExpired(token: string, skewSeconds = 30): boolean {
  const exp = jwtExpUnix(token);
  if (exp == null) return true;
  const now = Math.floor(Date.now() / 1000);
  return exp <= now + skewSeconds;
}
