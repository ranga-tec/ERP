import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { ISS_TOKEN_COOKIE, issApiBaseUrl } from "@/lib/env";

export const runtime = "nodejs";

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

  const headers = new Headers(req.headers);
  headers.delete("host");
  headers.delete("cookie");
  if (token) {
    headers.set("authorization", `Bearer ${token}`);
  } else {
    headers.delete("authorization");
  }

  const init: RequestInit = {
    method: req.method,
    headers,
    redirect: "manual",
  };

  if (req.method !== "GET" && req.method !== "HEAD") {
    init.body = await req.arrayBuffer();
  }

  let upstreamResp: Response;
  try {
    upstreamResp = await fetch(upstreamUrl, init);
  } catch (err) {
    return NextResponse.json(
      { error: "Upstream request failed.", detail: String(err) },
      { status: 502 },
    );
  }

  const body = upstreamResp.status === 204 ? null : await upstreamResp.arrayBuffer();

  const responseHeaders = new Headers();
  const contentType = upstreamResp.headers.get("content-type");
  if (contentType) responseHeaders.set("content-type", contentType);
  const contentDisposition = upstreamResp.headers.get("content-disposition");
  if (contentDisposition) responseHeaders.set("content-disposition", contentDisposition);

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
