/**
 * The API returns reservation and slot times as ISO 8601 in the COMPANY's local time with that zone's
 * offset (e.g. "2030-01-07T09:00:00-03:00"). The browser's own time zone must not change what is shown:
 * `new Date(iso).toLocaleTimeString()` would convert to the viewer's zone.
 */
const ISO_WITH_OFFSET =
  /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})(?::(\d{2}))?(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})$/i;

/**
 * A Date whose LOCAL fields (getHours(), toLocaleTimeString(), ...) read the wall-clock time written in
 * `iso`, i.e. the company's local time. It is for display and calendar grouping only: it is not the
 * instant, so never send it back to the API (post the original string instead). A string that does not
 * match the API format falls back to the regular `new Date(iso)`.
 */
export function toCompanyLocalDate(iso: string): Date {
  const match = ISO_WITH_OFFSET.exec(iso);
  if (!match) {
    return new Date(iso);
  }
  const [, year, month, day, hour, minute, second] = match;
  return new Date(
    Number(year),
    Number(month) - 1,
    Number(day),
    Number(hour),
    Number(minute),
    Number(second ?? 0),
  );
}

/** "YYYY-MM-DD" for the local calendar day of `date` (unlike `toISOString()`, never shifted through UTC). */
export function toDateInputValue(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}
