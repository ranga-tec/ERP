import { neuedgeApiBaseUrl } from "@/lib/env";

export const dynamic = "force-dynamic";

export async function GET() {
  try {
    const response = await fetch(`${neuedgeApiBaseUrl()}/health`, {
      cache: "no-store",
      signal: AbortSignal.timeout(10_000),
    });
    const body = await response.text();

    return new Response(body, {
      status: response.status,
      headers: {
        "content-type": response.headers.get("content-type") ?? "text/plain; charset=utf-8",
      },
    });
  } catch {
    return new Response("Unhealthy", { status: 503 });
  }
}
