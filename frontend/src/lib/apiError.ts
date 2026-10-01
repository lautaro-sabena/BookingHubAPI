/**
 * Extracts a user-facing message from a failed API call.
 *
 * The API answers errors with RFC 7807 problem details: `detail` holds the message; validation
 * failures carry per-field messages in `errors` instead; `title` is the generic reason phrase.
 * The legacy `{ error }` body is still read so older deployments keep working.
 */
export function getApiErrorMessage(err: unknown, fallback = "Something went wrong. Please try again."): string {
  const data = (err as { response?: { data?: unknown } } | null | undefined)?.response?.data;
  if (!data || typeof data !== "object") return fallback;

  const body = data as {
    detail?: unknown;
    errors?: unknown;
    title?: unknown;
    error?: unknown;
  };

  return (
    nonEmpty(body.detail) ??
    firstValidationMessage(body.errors) ??
    nonEmpty(body.title) ??
    nonEmpty(body.error) ??
    fallback
  );
}

function nonEmpty(value: unknown): string | undefined {
  return typeof value === "string" && value.trim() !== "" ? value : undefined;
}

function firstValidationMessage(errors: unknown): string | undefined {
  if (!errors || typeof errors !== "object") return undefined;
  for (const messages of Object.values(errors)) {
    if (Array.isArray(messages)) {
      const first = messages.find((m) => nonEmpty(m));
      if (first) return first as string;
    }
  }
  return undefined;
}
