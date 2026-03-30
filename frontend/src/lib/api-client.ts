function backendUrl(path: string): string {
  const trimmed = path.startsWith("/") ? path.slice(1) : path;
  return `/api/backend/${trimmed}`;
}

function extractErrorMessage(text: string): string {
  if (!text) return text;

  try {
    const parsed = JSON.parse(text) as {
      detail?: unknown;
      error?: unknown;
      title?: unknown;
      errors?: unknown;
    };

    if (typeof parsed.detail === "string" && parsed.detail.trim().length > 0) {
      return parsed.detail;
    }

    if (typeof parsed.error === "string" && parsed.error.trim().length > 0) {
      return parsed.error;
    }

    if (Array.isArray(parsed.errors)) {
      const errors = parsed.errors.filter((value): value is string => typeof value === "string" && value.trim().length > 0);
      if (errors.length > 0) {
        return errors.join(" ");
      }
    }

    if (typeof parsed.title === "string" && parsed.title.trim().length > 0) {
      return parsed.title;
    }
  } catch {
    // Fall back to the raw response body when the payload is not JSON.
  }

  return text;
}

async function ensureOk(resp: Response): Promise<Response> {
  if (resp.ok) return resp;
  const text = await resp.text();
  throw new Error(extractErrorMessage(text) || `${resp.status} ${resp.statusText}`);
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

export async function apiPostForm<T>(path: string, body: FormData): Promise<T> {
  const resp = await ensureOk(
    await fetch(backendUrl(path), {
      method: "POST",
      body,
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

export async function apiPutNoContent(path: string, body: unknown): Promise<void> {
  await ensureOk(
    await fetch(backendUrl(path), {
      method: "PUT",
      headers: { "content-type": "application/json" },
      body: JSON.stringify(body),
    }),
  );
}

export async function apiDeleteNoContent(path: string): Promise<void> {
  await ensureOk(
    await fetch(backendUrl(path), {
      method: "DELETE",
    }),
  );
}
