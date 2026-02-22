function backendUrl(path: string): string {
  const trimmed = path.startsWith("/") ? path.slice(1) : path;
  return `/api/backend/${trimmed}`;
}

async function ensureOk(resp: Response): Promise<Response> {
  if (resp.ok) return resp;
  const text = await resp.text();
  throw new Error(text || `${resp.status} ${resp.statusText}`);
}

export async function apiGet<T>(path: string): Promise<T> {
  const resp = await ensureOk(await fetch(backendUrl(path), { method: "GET" }));
  return (await resp.json()) as T;
}

export async function apiPost<T>(path: string, body: unknown): Promise<T> {
  const resp = await ensureOk(
    await fetch(backendUrl(path), {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify(body),
    }),
  );
  return (await resp.json()) as T;
}

export async function apiPostNoContent(path: string, body: unknown): Promise<void> {
  await ensureOk(
    await fetch(backendUrl(path), {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify(body),
    }),
  );
}

export async function apiPut<T>(path: string, body: unknown): Promise<T> {
  const resp = await ensureOk(
    await fetch(backendUrl(path), {
      method: "PUT",
      headers: { "content-type": "application/json" },
      body: JSON.stringify(body),
    }),
  );
  return (await resp.json()) as T;
}

