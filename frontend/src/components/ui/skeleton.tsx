import { cn } from "@/lib/utils";

export function Skeleton({ className }: { className?: string }) {
  return <div aria-hidden="true" className={cn("animate-pulse rounded-lg bg-muted", className)} />;
}

/**
 * Loading placeholder for a whole page. It keeps a visually hidden "Loading..." so assistive tech (and tests that
 * look for it) still find the same text the plain placeholder used to show.
 */
export function LoadingState({ variant = "page", className }: { variant?: "page" | "list" | "form" | "screen"; className?: string }) {
  if (variant === "screen") {
    return (
      <div role="status" className={cn("flex min-h-screen items-center justify-center", className)}>
        <div className="flex flex-col items-center gap-4">
          <span className="relative flex h-10 w-10 items-center justify-center rounded-xl bg-primary text-primary-foreground shadow-card">
            <span className="h-3 w-3 rounded-full bg-primary-foreground animate-pulse" aria-hidden="true" />
          </span>
          <span className="text-sm text-muted-foreground">Loading...</span>
        </div>
      </div>
    );
  }

  return (
    <div role="status" className={cn("space-y-6", className)}>
      <span className="sr-only">Loading...</span>
      <div className="space-y-2">
        <Skeleton className="h-8 w-56" />
        <Skeleton className="h-4 w-80 max-w-full" />
      </div>
      {variant === "form" ? (
        <div className="max-w-2xl space-y-4 rounded-xl border border-border/70 bg-card p-6">
          {[0, 1, 2, 3].map((i) => (
            <div key={i} className="space-y-2">
              <Skeleton className="h-4 w-28" />
              <Skeleton className="h-10 w-full" />
            </div>
          ))}
        </div>
      ) : variant === "list" ? (
        <div className="space-y-3">
          {[0, 1, 2, 3].map((i) => (
            <Skeleton key={i} className="h-20 w-full rounded-xl" />
          ))}
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {[0, 1, 2].map((i) => (
            <Skeleton key={i} className="h-40 rounded-xl" />
          ))}
        </div>
      )}
    </div>
  );
}
