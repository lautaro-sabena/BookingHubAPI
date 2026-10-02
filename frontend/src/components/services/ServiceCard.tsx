import { Clock } from "lucide-react";
import { cn } from "@/lib/utils";

/** Deterministic soft tint per service so the catalogue doesn't look like a wall of identical cards. */
const TINTS = [
  "from-accent to-primary/25",
  "from-secondary to-primary/15",
  "from-accent to-success/20",
  "from-muted to-primary/20",
];

function tintFor(key: string) {
  let h = 0;
  for (let i = 0; i < key.length; i++) h = (h * 31 + key.charCodeAt(i)) >>> 0;
  return TINTS[h % TINTS.length];
}

/** Cover band with the service's initial, used at the top of service cards. */
export function ServiceCover({ id, name, children }: { id: string; name: string; children?: React.ReactNode }) {
  return (
    <div className={cn("relative h-24 bg-gradient-to-br", tintFor(id))}>
      <span
        aria-hidden="true"
        className="absolute -bottom-6 left-5 flex h-12 w-12 items-center justify-center rounded-2xl border-4 border-card bg-primary text-lg font-bold text-primary-foreground shadow-card"
      >
        {name.charAt(0).toUpperCase()}
      </span>
      {children}
    </div>
  );
}

/** Price and duration chips. */
export function ServiceMeta({ price, durationMinutes }: { price: number; durationMinutes: number }) {
  return (
    <div className="flex flex-wrap items-center gap-2">
      <span className="text-lg font-bold tabular">${price}</span>
      <span className="inline-flex items-center gap-1 rounded-full bg-secondary px-2.5 py-0.5 text-xs font-medium text-secondary-foreground tabular">
        <Clock className="h-3 w-3" aria-hidden="true" />
        {durationMinutes} min
      </span>
    </div>
  );
}
