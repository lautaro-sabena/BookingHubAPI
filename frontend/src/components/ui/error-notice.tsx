import { getApiErrorMessage } from "@/lib/apiError";

/** The red message box used for failed requests. Renders nothing without an error. */
export function ErrorNotice({ error, fallback }: { error: unknown; fallback?: string }) {
  if (!error) return null;
  return (
    <div role="alert" className="p-3 text-sm text-red-500 bg-red-50 rounded-md">
      {getApiErrorMessage(error, fallback)}
    </div>
  );
}
