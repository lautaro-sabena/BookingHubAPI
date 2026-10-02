import { CalendarCheck, Clock, Star } from "lucide-react";
import { Logo } from "@/components/layout/Logo";
import { ThemeToggle } from "@/components/layout/ThemeToggle";

const POINTS = [
  { icon: CalendarCheck, text: "Book and manage appointments in a few taps" },
  { icon: Clock, text: "Times always shown in the business's time zone" },
  { icon: Star, text: "Save your favorite services for next time" },
];

/** Two-column frame for login and register: brand panel on large screens, the form card on the right. */
export function AuthShell({ children }: { children: React.ReactNode }) {
  return (
    <div className="grid min-h-screen lg:grid-cols-[minmax(0,1fr)_minmax(0,1.1fr)]">
      <aside className="relative hidden overflow-hidden bg-primary p-10 text-primary-foreground lg:flex lg:flex-col lg:justify-between">
        <div
          aria-hidden="true"
          className="pointer-events-none absolute inset-0 opacity-[0.12] [background-image:radial-gradient(currentColor_1px,transparent_1px)] [background-size:22px_22px]"
        />
        <Logo className="relative [&>span]:bg-primary-foreground [&>span]:text-primary" />
        <div className="relative space-y-6">
          <p className="max-w-sm text-3xl font-bold leading-tight tracking-tight">
            Your next appointment is a few clicks away.
          </p>
          <ul className="space-y-3">
            {POINTS.map(({ icon: Icon, text }) => (
              <li key={text} className="flex items-center gap-3 text-sm opacity-90">
                <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary-foreground/15">
                  <Icon className="h-4 w-4" aria-hidden="true" />
                </span>
                {text}
              </li>
            ))}
          </ul>
        </div>
        <p className="relative text-xs opacity-70">© {new Date().getFullYear()} BookingHub</p>
      </aside>
      <div className="flex flex-col">
        <div className="flex h-16 items-center justify-between px-4 sm:px-6">
          <Logo className="lg:invisible" />
          <ThemeToggle />
        </div>
        <div className="flex flex-1 items-center justify-center px-4 pb-16 pt-4 sm:px-6">
          <div className="w-full max-w-md animate-in fade-in-0 slide-in-from-bottom-2 duration-500">{children}</div>
        </div>
      </div>
    </div>
  );
}
