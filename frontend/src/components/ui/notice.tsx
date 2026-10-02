import { CheckCircle2 } from "lucide-react";
import { cn } from "@/lib/utils";

/** Positive confirmation after a save or booking. The error counterpart is ErrorNotice. */
export function SuccessNotice({ children, className }: { children: React.ReactNode; className?: string }) {
  return (
    <div
      role="status"
      className={cn(
        "flex items-start gap-3 rounded-lg border border-success/25 bg-success-soft px-4 py-3 text-sm font-medium text-success animate-in fade-in-0 slide-in-from-top-1",
        className
      )}
    >
      <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" />
      <span className="min-w-0">{children}</span>
    </div>
  );
}

/** Inline form error that is not an API error (e.g. "Passwords do not match"). */
export function FormError({ children }: { children: React.ReactNode }) {
  return (
    <div
      role="alert"
      className="rounded-lg border border-destructive/25 bg-destructive-soft px-4 py-3 text-sm text-destructive"
    >
      {children}
    </div>
  );
}
