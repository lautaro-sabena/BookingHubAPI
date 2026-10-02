import { AlertCircle } from "lucide-react";
import { getApiErrorMessage } from "@/lib/apiError";

/** The message box used for failed requests. Renders nothing without an error. */
export function ErrorNotice({ error, fallback }: { error: unknown; fallback?: string }) {
  if (!error) return null;
  return (
    <div
      role="alert"
      className="flex items-start gap-3 rounded-lg border border-destructive/25 bg-destructive-soft px-4 py-3 text-sm text-destructive animate-in fade-in-0 slide-in-from-top-1"
    >
      <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" />
      <span className="min-w-0">{getApiErrorMessage(error, fallback)}</span>
    </div>
  );
}
