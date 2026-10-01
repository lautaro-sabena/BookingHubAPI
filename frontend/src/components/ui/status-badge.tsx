import { getStatusClasses } from "@/lib/calendar";

export function StatusBadge({ status }: { status: string }) {
  return <span className={`px-2 py-1 text-xs rounded ${getStatusClasses(status)}`}>{status}</span>;
}
