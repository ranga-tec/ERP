import { cookies } from "next/headers";
import { NEUEDGE_TOKEN_COOKIE, neuedgeApiBaseUrl } from "@/lib/env";

export class BackendHttpError extends Error {
  constructor(
    public readonly status: number,
    public readonly responseBody: string,
    public readonly path: string,
  ) {
    super(`Backend ${status}: ${responseBody}`);
    this.name = "BackendHttpError";
  }
}

export async function backendFetchJson<T>(
  path: string,
  init?: RequestInit,
): Promise<T> {
  const cookieStore = await cookies();
  const token = cookieStore.get(NEUEDGE_TOKEN_COOKIE)?.value;
  const url = new URL(`/api${path.startsWith("/") ? path : `/${path}`}`, neuedgeApiBaseUrl());

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
    throw new BackendHttpError(resp.status, text, path);
  }

  return (await resp.json()) as T;
}
