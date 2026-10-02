import { getStatusClasses } from "@/lib/calendar";
import { cn } from "@/lib/utils";

const STATUS_STYLES: Record<string, string> = {
  pending: "bg-warning-soft text-warning",
  confirmed: "bg-success-soft text-success",
  completed: "bg-accent text-accent-foreground",
  cancelled: "bg-destructive-soft text-destructive",
  canceled: "bg-destructive-soft text-destructive",
};

/** Reservation status pill. Unknown statuses fall back to the shared calendar colors. */
export function StatusBadge({ status, className }: { status: string; className?: string }) {
  const known = STATUS_STYLES[status.toLowerCase()];
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 whitespace-nowrap rounded-full px-2.5 py-0.5 text-xs font-semibold",
        known ?? getStatusClasses(status),
        className
      )}
    >
      <span className="h-1.5 w-1.5 rounded-full bg-current" aria-hidden="true" />
      {status}
    </span>
  );
}
