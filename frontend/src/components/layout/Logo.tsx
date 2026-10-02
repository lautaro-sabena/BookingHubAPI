import Link from "next/link";
import { cn } from "@/lib/utils";

/** BookingHub wordmark: a calendar-tab mark in the brand teal plus the name. */
export function Logo({ className, href = "/" }: { className?: string; href?: string }) {
  return (
    <Link
      href={href}
      className={cn(
        "inline-flex items-center gap-2.5 rounded-lg text-lg font-extrabold tracking-tight focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 ring-offset-background",
        className
      )}
    >
      <LogoMark />
      BookingHub
    </Link>
  );
}

export function LogoMark({ className }: { className?: string }) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "relative flex h-8 w-8 items-center justify-center rounded-[9px] bg-primary text-primary-foreground shadow-sm",
        className
      )}
    >
      <svg viewBox="0 0 24 24" className="h-[18px] w-[18px]" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
        <rect x="4" y="5.5" width="16" height="14" rx="3" />
        <path d="M8 3.5v4M16 3.5v4M8.5 13.5l2.5 2.5 4.5-4.5" />
      </svg>
    </span>
  );
}
