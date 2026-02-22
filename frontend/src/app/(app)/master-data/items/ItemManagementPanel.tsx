"use client";

import { useEffect, useMemo, useState } from "react";
import { apiDeleteNoContent, apiGet, apiPostForm } from "@/lib/api-client";
import {
  Button,
  SecondaryButton,
  SecondaryLink,
  Select,
  Table,
  Textarea,
} from "@/components/ui";

type ItemRef = { id: string; sku: string; name: string };

type AttachmentDto = {
  id: string;
  itemId: string;
  fileName: string;
  url: string;
  isImage: boolean;
  contentType?: string | null;
  sizeBytes?: number | null;
  notes?: string | null;
  createdAt: string;
  createdBy?: string | null;
};

type PriceHistoryDto = {
  auditLogId: string;
  occurredAt: string;
  userId?: string | null;
  oldDefaultUnitCost?: number | null;
  newDefaultUnitCost: number;
};

function toClientAttachmentUrl(url: string): string {
  if (url.startsWith("/api/")) {
    return `/api/backend/${url.slice("/api/".length)}`;
  }

  if (url.startsWith("api/")) {
    return `/api/backend/${url.slice("api/".length)}`;
  }

  return url;
}

export function ItemManagementPanel({ items }: { items: ItemRef[] }) {
  const sortedItems = useMemo(
    () => items.slice().sort((a, b) => a.sku.localeCompare(b.sku)),
    [items],
  );
  const [itemId, setItemId] = useState(sortedItems[0]?.id ?? "");
  const [attachments, setAttachments] = useState<AttachmentDto[]>([]);
  const [priceHistory, setPriceHistory] = useState<PriceHistoryDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [uploadFile, setUploadFile] = useState<File | null>(null);
  const [notes, setNotes] = useState("");
  const [saving, setSaving] = useState(false);
  const [deleteBusyId, setDeleteBusyId] = useState<string | null>(null);

  async function loadForSelectedItem(selectedItemId: string) {
    if (!selectedItemId) {
      setAttachments([]);
      setPriceHistory([]);
      return;
    }

    const [nextAttachments, nextPriceHistory] = await Promise.all([
      apiGet<AttachmentDto[]>(`items/${selectedItemId}/attachments`),
      apiGet<PriceHistoryDto[]>(`items/${selectedItemId}/price-history`),
    ]);
    setAttachments(nextAttachments);
    setPriceHistory(nextPriceHistory);
  }

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setLoading(true);
      setError(null);
      try {
        if (!itemId) {
          setAttachments([]);
          setPriceHistory([]);
          return;
        }

        const [nextAttachments, nextPriceHistory] = await Promise.all([
          apiGet<AttachmentDto[]>(`items/${itemId}/attachments`),
          apiGet<PriceHistoryDto[]>(`items/${itemId}/price-history`),
        ]);
        if (cancelled) return;
        setAttachments(nextAttachments);
        setPriceHistory(nextPriceHistory);
      } catch (err) {
        if (cancelled) return;
        setError(err instanceof Error ? err.message : String(err));
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    void load();
    return () => {
      cancelled = true;
    };
  }, [itemId]);

  const selectedItem = sortedItems.find((i) => i.id === itemId);

  async function onUploadAttachment(e: React.FormEvent) {
    e.preventDefault();
    if (!itemId) return;
    if (!uploadFile) {
      setError("Select a file to upload.");
      return;
    }

    setError(null);
    setSaving(true);
    try {
      const formData = new FormData();
      formData.set("file", uploadFile);
      if (notes.trim()) {
        formData.set("notes", notes.trim());
      }

      const created = await apiPostForm<AttachmentDto>(`items/${itemId}/attachments/upload`, formData);
      setAttachments((prev) => [created, ...prev]);
      setUploadFile(null);
      setNotes("");
      const fileInput = document.getElementById("item-attachment-file") as HTMLInputElement | null;
      if (fileInput) {
        fileInput.value = "";
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setSaving(false);
    }
  }

  async function onDeleteAttachment(attachmentId: string) {
    if (!itemId) return;

    setError(null);
    setDeleteBusyId(attachmentId);
    try {
      await apiDeleteNoContent(`items/${itemId}/attachments/${attachmentId}`);
      setAttachments((prev) => prev.filter((a) => a.id !== attachmentId));
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setDeleteBusyId(null);
    }
  }

  async function onReload() {
    if (!itemId) return;
    setLoading(true);
    setError(null);
    try {
      await loadForSelectedItem(itemId);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="space-y-4">
      <div className="grid gap-3 md:grid-cols-[minmax(0,1fr)_auto] md:items-end">
        <div>
          <label className="mb-1 block text-sm font-medium">Item</label>
          <Select
            value={itemId}
            onChange={(e) => setItemId(e.target.value)}
            disabled={sortedItems.length === 0}
          >
            {sortedItems.length === 0 ? <option value="">No items</option> : null}
            {sortedItems.map((i) => (
              <option key={i.id} value={i.id}>
                {i.sku} - {i.name}
              </option>
            ))}
          </Select>
        </div>
        <SecondaryButton type="button" onClick={() => void onReload()} disabled={!itemId || loading}>
          {loading ? "Loading..." : "Reload"}
        </SecondaryButton>
      </div>

      {selectedItem ? (
        <div className="rounded-md border border-zinc-200 p-3 text-sm dark:border-zinc-800">
          <div className="font-medium">
            {selectedItem.sku} - {selectedItem.name}
          </div>
          <div className="mt-1 text-xs text-zinc-500">
            File uploads are stored on the backend server. Price history is derived from audit logs.
          </div>
        </div>
      ) : null}

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      {sortedItems.length === 0 ? (
        <div className="text-sm text-zinc-500">Create an item first.</div>
      ) : (
        <>
          <div className="grid gap-6 xl:grid-cols-2">
            <div className="space-y-3">
              <div className="text-sm font-semibold">Upload Attachment / Image</div>
              <form onSubmit={onUploadAttachment} className="space-y-3">
                <div>
                  <label className="mb-1 block text-sm font-medium">File</label>
                  <input
                    id="item-attachment-file"
                    type="file"
                    onChange={(e) => setUploadFile(e.target.files?.[0] ?? null)}
                    className="w-full rounded-md border border-zinc-300 bg-white px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-zinc-900/10 dark:border-zinc-700 dark:bg-zinc-900 dark:focus:ring-zinc-100/10"
                    required
                  />
                  <div className="mt-1 text-xs text-zinc-500">
                    Max upload size: 25 MB.
                  </div>
                </div>
                <div>
                  <label className="mb-1 block text-sm font-medium">Notes</label>
                  <Textarea value={notes} onChange={(e) => setNotes(e.target.value)} />
                </div>
                <Button type="submit" disabled={saving || !itemId || !uploadFile}>
                  {saving ? "Uploading..." : "Upload Attachment"}
                </Button>
              </form>
            </div>

            <div className="space-y-3">
              <div className="text-sm font-semibold">Price History</div>
              <div className="overflow-auto">
                <Table>
                  <thead>
                    <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                      <th className="py-2 pr-3">When</th>
                      <th className="py-2 pr-3">Old Cost</th>
                      <th className="py-2 pr-3">New Cost</th>
                      <th className="py-2 pr-3">User</th>
                    </tr>
                  </thead>
                  <tbody>
                    {priceHistory.map((entry) => (
                      <tr key={entry.auditLogId} className="border-b border-zinc-100 dark:border-zinc-900">
                        <td className="py-2 pr-3">{new Date(entry.occurredAt).toLocaleString()}</td>
                        <td className="py-2 pr-3">
                          {entry.oldDefaultUnitCost == null ? (
                            <span className="text-zinc-500">Initial</span>
                          ) : (
                            entry.oldDefaultUnitCost
                          )}
                        </td>
                        <td className="py-2 pr-3">{entry.newDefaultUnitCost}</td>
                        <td className="py-2 pr-3 font-mono text-xs text-zinc-500">
                          {entry.userId ?? "-"}
                        </td>
                      </tr>
                    ))}
                    {priceHistory.length === 0 ? (
                      <tr>
                        <td className="py-6 text-sm text-zinc-500" colSpan={4}>
                          No default cost changes captured yet.
                        </td>
                      </tr>
                    ) : null}
                  </tbody>
                </Table>
              </div>
            </div>
          </div>

          <div className="space-y-3">
            <div className="text-sm font-semibold">Attachment List</div>
            <div className="overflow-auto">
              <Table>
                <thead>
                  <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                    <th className="py-2 pr-3">Preview</th>
                    <th className="py-2 pr-3">File</th>
                    <th className="py-2 pr-3">Type</th>
                    <th className="py-2 pr-3">Size</th>
                    <th className="py-2 pr-3">Notes</th>
                    <th className="py-2 pr-3">Created</th>
                    <th className="py-2 pr-3">Action</th>
                  </tr>
                </thead>
                <tbody>
                  {attachments.map((attachment) => {
                    const clientUrl = toClientAttachmentUrl(attachment.url);
                    return (
                      <tr key={attachment.id} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                        <td className="py-2 pr-3">
                          {attachment.isImage ? (
                            // eslint-disable-next-line @next/next/no-img-element
                            <img
                              src={clientUrl}
                              alt={attachment.fileName}
                              className="h-14 w-14 rounded border border-zinc-200 object-cover dark:border-zinc-800"
                            />
                          ) : (
                            <span className="text-zinc-500">-</span>
                          )}
                        </td>
                        <td className="py-2 pr-3">
                          <div className="font-medium">{attachment.fileName}</div>
                          <div className="mt-1">
                            <SecondaryLink
                              href={clientUrl}
                              target="_blank"
                              rel="noopener noreferrer"
                              className="px-2 py-1 text-xs"
                            >
                              Open
                            </SecondaryLink>
                          </div>
                        </td>
                        <td className="py-2 pr-3">
                          <div>{attachment.isImage ? "Image" : "Attachment"}</div>
                          <div className="text-xs text-zinc-500">{attachment.contentType ?? "-"}</div>
                        </td>
                        <td className="py-2 pr-3 text-xs font-mono">
                          {attachment.sizeBytes == null ? "-" : attachment.sizeBytes}
                        </td>
                        <td className="py-2 pr-3 text-zinc-500">{attachment.notes ?? "-"}</td>
                        <td className="py-2 pr-3 text-xs text-zinc-500">
                          {new Date(attachment.createdAt).toLocaleString()}
                        </td>
                        <td className="py-2 pr-3">
                          <SecondaryButton
                            type="button"
                            onClick={() => void onDeleteAttachment(attachment.id)}
                            disabled={deleteBusyId === attachment.id}
                            className="px-2 py-1 text-xs"
                          >
                            {deleteBusyId === attachment.id ? "Deleting..." : "Delete"}
                          </SecondaryButton>
                        </td>
                      </tr>
                    );
                  })}
                  {attachments.length === 0 ? (
                    <tr>
                      <td className="py-6 text-sm text-zinc-500" colSpan={7}>
                        No attachments/images for this item yet.
                      </td>
                    </tr>
                  ) : null}
                </tbody>
              </Table>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
