import { cookies } from "next/headers";
import { ISS_TOKEN_COOKIE, issApiBaseUrl } from "@/lib/env";

export async function backendFetchJson<T>(
  path: string,
  init?: RequestInit,
): Promise<T> {
  const cookieStore = await cookies();
  const token = cookieStore.get(ISS_TOKEN_COOKIE)?.value;
  const url = new URL(`/api${path.startsWith("/") ? path : `/${path}`}`, issApiBaseUrl());

  const headers = new Headers(init?.headers);
  headers.set("accept", "application/json");
  if (token) {
    headers.set("authorization", `Bearer ${token}`);
  }

  const resp = await fetch(url, {
    ...init,
    headers,
    cache: "no-store",
  });

  if (!resp.ok) {
    const text = await resp.text();
    throw new Error(`Backend ${resp.status}: ${text}`);
  }

  return (await resp.json()) as T;
}
