"use client";

import { useEffect, useState } from "react";
import {
  apiDeleteNoContent,
  apiGet,
  apiPost,
  apiPostForm,
} from "@/lib/api-client";
import { Button, Card, Input, SecondaryButton, SecondaryLink, Table, Textarea } from "@/components/ui";

type CommentDto = {
  id: string;
  referenceType: string;
  referenceId: string;
  text: string;
  createdAt: string;
  createdBy?: string | null;
  lastModifiedAt?: string | null;
  lastModifiedBy?: string | null;
};

type AttachmentDto = {
  id: string;
  referenceType: string;
  referenceId: string;
  fileName: string;
  url: string;
  isImage: boolean;
  contentType?: string | null;
  sizeBytes?: number | null;
  notes?: string | null;
  createdAt: string;
  createdBy?: string | null;
};

function toClientAttachmentUrl(url: string) {
  if (url.startsWith("/api/")) {
    return `/api/backend/${url.slice("/api/".length)}`;
  }
  return url;
}

export function DocumentCollaborationPanel({
  referenceType,
  referenceId,
  title = "Comments & Attachments",
}: {
  referenceType: string;
  referenceId: string;
  title?: string;
}) {
  const [comments, setComments] = useState<CommentDto[]>([]);
  const [attachments, setAttachments] = useState<AttachmentDto[]>([]);
  const [commentText, setCommentText] = useState("");
  const [attachmentNotes, setAttachmentNotes] = useState("");
  const [busy, setBusy] = useState(false);
  const [uploadBusy, setUploadBusy] = useState(false);
  const [deleteBusyKey, setDeleteBusyKey] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  async function load() {
    const [commentsResp, attachmentsResp] = await Promise.all([
      apiGet<CommentDto[]>(`documents/${referenceType}/${referenceId}/comments`),
      apiGet<AttachmentDto[]>(`documents/${referenceType}/${referenceId}/attachments`),
    ]);
    setComments(commentsResp);
    setAttachments(attachmentsResp);
  }

  useEffect(() => {
    void load().catch((err) => setError(err instanceof Error ? err.message : String(err)));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [referenceType, referenceId]);

  async function addComment() {
    if (!commentText.trim()) return;
    setError(null);
    setBusy(true);
    try {
      await apiPost<CommentDto>(`documents/${referenceType}/${referenceId}/comments`, {
        text: commentText.trim(),
      });
      setCommentText("");
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  async function uploadAttachment() {
    const file = selectedFile;
    if (!file) {
      setError("Select a file to upload.");
      return;
    }

    setError(null);
    setUploadBusy(true);
    try {
      const form = new FormData();
      form.append("file", file);
      if (attachmentNotes.trim()) {
        form.append("notes", attachmentNotes.trim());
      }
      await apiPostForm<AttachmentDto>(`documents/${referenceType}/${referenceId}/attachments/upload`, form);
      setSelectedFile(null);
      setAttachmentNotes("");
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setUploadBusy(false);
    }
  }

  async function deleteComment(commentId: string) {
    setError(null);
    setDeleteBusyKey(`comment:${commentId}`);
    try {
      await apiDeleteNoContent(`documents/${referenceType}/${referenceId}/comments/${commentId}`);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setDeleteBusyKey(null);
    }
  }

  async function deleteAttachment(attachmentId: string) {
    setError(null);
    setDeleteBusyKey(`attachment:${attachmentId}`);
    try {
      await apiDeleteNoContent(`documents/${referenceType}/${referenceId}/attachments/${attachmentId}`);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setDeleteBusyKey(null);
    }
  }

  return (
    <div className="space-y-4">
      <Card>
        <div className="mb-3 text-sm font-semibold">{title}</div>
        <div className="grid gap-4 lg:grid-cols-2">
          <div className="space-y-3">
            <div className="text-sm font-medium">Comments</div>
            <Textarea
              value={commentText}
              onChange={(e) => setCommentText(e.target.value)}
              placeholder="Add notes, handover remarks, approvals, customer communication notes..."
            />
            <div className="flex gap-2">
              <Button type="button" onClick={addComment} disabled={busy || !commentText.trim()}>
                {busy ? "Adding..." : "Add Comment"}
              </Button>
            </div>
            <div className="space-y-2">
              {comments.map((comment) => (
                <div key={comment.id} className="rounded-lg border border-zinc-200 p-3 dark:border-zinc-800">
                  <div className="whitespace-pre-wrap text-sm text-zinc-800 dark:text-zinc-100">{comment.text}</div>
                  <div className="mt-2 flex flex-wrap items-center gap-2 text-xs text-zinc-500">
                    <span>{new Date(comment.createdAt).toLocaleString()}</span>
                    {comment.createdBy ? <span className="font-mono">{comment.createdBy}</span> : null}
                    <SecondaryButton
                      type="button"
                      className="px-2 py-1 text-xs"
                      disabled={deleteBusyKey === `comment:${comment.id}`}
                      onClick={() => void deleteComment(comment.id)}
                    >
                      {deleteBusyKey === `comment:${comment.id}` ? "Deleting..." : "Delete"}
                    </SecondaryButton>
                  </div>
                </div>
              ))}
              {comments.length === 0 ? (
                <div className="text-sm text-zinc-500">No comments yet.</div>
              ) : null}
            </div>
          </div>

          <div className="space-y-3">
            <div className="text-sm font-medium">Attachments</div>
            <div className="space-y-2 rounded-lg border border-zinc-200 p-3 dark:border-zinc-800">
              <input
                type="file"
                onChange={(e) => setSelectedFile(e.target.files?.[0] ?? null)}
                className="w-full rounded-md border border-zinc-300 bg-white px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-zinc-900/10 dark:border-zinc-700 dark:bg-zinc-900 dark:focus:ring-zinc-100/10"
              />
              <Input
                value={attachmentNotes}
                onChange={(e) => setAttachmentNotes(e.target.value)}
                placeholder="Optional notes"
              />
              <div className="flex gap-2">
                <Button type="button" onClick={uploadAttachment} disabled={uploadBusy}>
                  {uploadBusy ? "Uploading..." : "Upload Attachment"}
                </Button>
              </div>
            </div>

            <div className="overflow-auto">
              <Table>
                <thead>
                  <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                    <th className="py-2 pr-3">File</th>
                    <th className="py-2 pr-3">Type</th>
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
                          <div className="font-medium">{attachment.fileName}</div>
                          <div className="mt-1 flex flex-wrap items-center gap-2">
                            <SecondaryLink
                              href={clientUrl}
                              target="_blank"
                              rel="noopener noreferrer"
                              className="px-2 py-1 text-xs"
                            >
                              Open
                            </SecondaryLink>
                            {attachment.isImage ? (
                              // eslint-disable-next-line @next/next/no-img-element
                              <img
                                src={clientUrl}
                                alt={attachment.fileName}
                                className="h-10 w-10 rounded border border-zinc-200 object-cover dark:border-zinc-800"
                              />
                            ) : null}
                          </div>
                        </td>
                        <td className="py-2 pr-3 text-xs text-zinc-500">
                          <div>{attachment.contentType ?? "-"}</div>
                          <div>{attachment.sizeBytes ?? "-"} bytes</div>
                        </td>
                        <td className="py-2 pr-3 text-zinc-500">{attachment.notes ?? "-"}</td>
                        <td className="py-2 pr-3 text-xs text-zinc-500">
                          {new Date(attachment.createdAt).toLocaleString()}
                        </td>
                        <td className="py-2 pr-3">
                          <SecondaryButton
                            type="button"
                            onClick={() => void deleteAttachment(attachment.id)}
                            disabled={deleteBusyKey === `attachment:${attachment.id}`}
                            className="px-2 py-1 text-xs"
                          >
                            {deleteBusyKey === `attachment:${attachment.id}` ? "Deleting..." : "Delete"}
                          </SecondaryButton>
                        </td>
                      </tr>
                    );
                  })}
                  {attachments.length === 0 ? (
                    <tr>
                      <td className="py-6 text-sm text-zinc-500" colSpan={5}>
                        No attachments yet.
                      </td>
                    </tr>
                  ) : null}
                </tbody>
              </Table>
            </div>
          </div>
        </div>

        {error ? (
          <div className="mt-3 rounded-md border border-red-200 bg-red-50 p-2 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
            {error}
          </div>
        ) : null}
      </Card>
    </div>
  );
}
