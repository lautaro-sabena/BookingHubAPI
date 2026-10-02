import { CalendarClock } from "lucide-react";
import { StatusBadge } from "@/components/ui/status-badge";
import { cn } from "@/lib/utils";

interface ReservationRowProps {
  title: React.ReactNode;
  /** Already formatted in the company's time zone by the caller. */
  when: React.ReactNode;
  status: string;
  meta?: React.ReactNode;
  actions?: React.ReactNode;
  className?: string;
}

/** One reservation in a list: icon, service, time, status and optional actions. Layout only. */
export function ReservationRow({ title, when, status, meta, actions, className }: ReservationRowProps) {
  const inactive = status.toLowerCase().startsWith("cancel");
  return (
    <div
      className={cn(
        "flex flex-col gap-4 rounded-xl border border-border/70 bg-card p-4 shadow-card transition-colors sm:flex-row sm:items-center sm:justify-between",
        inactive && "bg-card/60",
        className
      )}
    >
      <div className="flex min-w-0 items-start gap-4">
        <span
          aria-hidden="true"
          className={cn(
            "flex h-11 w-11 shrink-0 items-center justify-center rounded-xl",
            inactive ? "bg-muted text-muted-foreground" : "bg-accent text-accent-foreground"
          )}
        >
          <CalendarClock className="h-5 w-5" />
        </span>
        <div className="min-w-0 space-y-1">
          <div className="flex flex-wrap items-center gap-2">
            <p className={cn("font-semibold", inactive && "text-muted-foreground")}>{title}</p>
            <StatusBadge status={status} />
          </div>
          <p className="text-sm text-muted-foreground tabular">{when}</p>
          {meta && <div className="text-sm text-muted-foreground">{meta}</div>}
        </div>
      </div>
      {actions && <div className="flex shrink-0 flex-wrap items-center gap-2 sm:justify-end">{actions}</div>}
    </div>
  );
}
