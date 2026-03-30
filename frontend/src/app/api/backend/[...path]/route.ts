import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { ISS_TOKEN_COOKIE, issApiBaseUrl } from "@/lib/env";

export const runtime = "nodejs";

const DEFAULT_UPSTREAM_TIMEOUT_MS = 30_000;
const REQUEST_HEADER_ALLOWLIST = new Set([
  "accept",
  "content-type",
  "content-disposition",
  "range",
  "if-none-match",
  "if-modified-since",
]);
const HOP_BY_HOP_HEADERS = new Set([
  "connection",
  "expect",
  "keep-alive",
  "proxy-authenticate",
  "proxy-authorization",
  "te",
  "trailer",
  "transfer-encoding",
  "upgrade",
]);

function upstreamTimeoutMs(): number {
  const raw = process.env.ISS_BACKEND_PROXY_TIMEOUT_MS;
  if (!raw) return DEFAULT_UPSTREAM_TIMEOUT_MS;

  const parsed = Number(raw);
  if (!Number.isFinite(parsed) || parsed <= 0) {
    return DEFAULT_UPSTREAM_TIMEOUT_MS;
  }

  return Math.trunc(parsed);
}

function buildUpstreamUrl(req: Request, path: string[]): URL {
  const incoming = new URL(req.url);
  const upstream = new URL(`/api/${path.join("/")}`, issApiBaseUrl());
  incoming.searchParams.forEach((value, key) => upstream.searchParams.append(key, value));
  return upstream;
}

async function forward(req: Request, path: string[]) {
  const upstreamUrl = buildUpstreamUrl(req, path);
  const cookieStore = await cookies();
  const token = cookieStore.get(ISS_TOKEN_COOKIE)?.value;

  const headers = new Headers();
  req.headers.forEach((value, key) => {
    const normalized = key.toLowerCase();
    if (!REQUEST_HEADER_ALLOWLIST.has(normalized)) {
      return;
    }

    headers.set(key, value);
  });

  if (token) {
    headers.set("authorization", `Bearer ${token}`);
  } else {
    headers.delete("authorization");
  }

  const controller = new AbortController();
  const timeoutHandle = setTimeout(() => controller.abort(), upstreamTimeoutMs());

  const init: RequestInit = {
    method: req.method,
    headers,
    redirect: "manual",
    signal: controller.signal,
  };

  let upstreamResp: Response;
  try {
    if (req.method !== "GET" && req.method !== "HEAD") {
      // Re-buffer request content instead of replaying the original stream so the upstream
      // fetch can compute its own transport headers and avoid unsupported proxy headers.
      const body = await req.arrayBuffer();
      if (body.byteLength > 0) {
        init.body = body;
      }
    }

    upstreamResp = await fetch(upstreamUrl, init);
  } catch (err) {
    clearTimeout(timeoutHandle);
    if (err instanceof Error && err.name === "AbortError") {
      return NextResponse.json(
        { error: "Upstream request timed out." },
        { status: 504 },
      );
    }

    const cause = err instanceof Error && "cause" in err
      ? String((err as Error & { cause?: unknown }).cause)
      : undefined;
    console.error("Backend proxy request failed", {
      method: req.method,
      upstreamUrl: upstreamUrl.toString(),
      error: String(err),
      cause,
    });

    return NextResponse.json(
      { error: "Upstream request failed.", detail: String(err) },
      { status: 502 },
    );
  } finally {
    clearTimeout(timeoutHandle);
  }

  const responseHeaders = new Headers();
  upstreamResp.headers.forEach((value, key) => {
    if (HOP_BY_HOP_HEADERS.has(key.toLowerCase())) {
      return;
    }
    responseHeaders.set(key, value);
  });

  const body = upstreamResp.status === 204 || req.method === "HEAD" ? null : upstreamResp.body;
  return new NextResponse(body, { status: upstreamResp.status, headers: responseHeaders });
}

export async function GET(req: Request, ctx: { params: Promise<{ path: string[] }> }) {
  const { path } = await ctx.params;
  return forward(req, path);
}

export async function POST(req: Request, ctx: { params: Promise<{ path: string[] }> }) {
  const { path } = await ctx.params;
  return forward(req, path);
}

export async function PUT(req: Request, ctx: { params: Promise<{ path: string[] }> }) {
  const { path } = await ctx.params;
  return forward(req, path);
}

export async function PATCH(req: Request, ctx: { params: Promise<{ path: string[] }> }) {
  const { path } = await ctx.params;
  return forward(req, path);
}

export async function DELETE(req: Request, ctx: { params: Promise<{ path: string[] }> }) {
  const { path } = await ctx.params;
  return forward(req, path);
}
