"use client";

import Link from "next/link";
import { ArrowRight, CalendarCheck, Clock, Star, Building2 } from "lucide-react";
import { Logo } from "@/components/layout/Logo";
import { ThemeToggle } from "@/components/layout/ThemeToggle";

const FEATURES = [
  {
    icon: CalendarCheck,
    title: "Book in seconds",
    text: "Pick a day, choose a free slot and confirm. No calls, no back and forth.",
  },
  {
    icon: Clock,
    title: "Always the right time",
    text: "Slots are shown in the business's own time zone, so nobody shows up an hour early.",
  },
  {
    icon: Building2,
    title: "Built for owners",
    text: "Publish services, set opening hours and confirm reservations from one calendar.",
  },
];

const PREVIEW_SLOTS = [
  { time: "09:00 AM", state: "free" },
  { time: "09:45 AM", state: "taken" },
  { time: "10:30 AM", state: "selected" },
  { time: "11:15 AM", state: "free" },
  { time: "02:00 PM", state: "free" },
  { time: "02:45 PM", state: "free" },
] as const;

/** Decorative booking card that shows what the product does. Hidden from assistive tech. */
function BookingPreview() {
  return (
    <div aria-hidden="true" className="relative mx-auto w-full max-w-md select-none">
      <div className="absolute -inset-6 -z-10 rounded-[2rem] bg-gradient-to-br from-accent via-transparent to-transparent opacity-80 blur-2xl" />
      <div className="rounded-2xl border border-border/70 bg-card p-5 shadow-lift">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-xs text-muted-foreground">Studio Norte</p>
            <p className="text-lg font-bold tracking-tight">Haircut &amp; beard trim</p>
          </div>
          <span className="flex h-8 w-8 items-center justify-center rounded-full bg-accent text-primary">
            <Star className="h-4 w-4 fill-current" />
          </span>
        </div>
        <div className="mt-3 flex gap-2">
          <span className="rounded-full bg-accent px-2.5 py-1 text-xs font-semibold text-accent-foreground">$25</span>
          <span className="rounded-full bg-accent px-2.5 py-1 text-xs font-semibold text-accent-foreground">45 min</span>
        </div>
        <div className="mt-5 grid grid-cols-5 gap-1.5 text-center">
          {[
            ["Mon", "5"],
            ["Tue", "6"],
            ["Wed", "7"],
            ["Thu", "8"],
            ["Fri", "9"],
          ].map(([d, n]) => (
            <div
              key={d}
              className={
                n === "8"
                  ? "rounded-lg bg-primary py-2 text-primary-foreground"
                  : "rounded-lg border border-border/70 bg-background py-2 text-muted-foreground"
              }
            >
              <div className="text-[10px]">{d}</div>
              <div className={n === "8" ? "text-sm font-bold" : "text-sm font-semibold text-foreground"}>{n}</div>
            </div>
          ))}
        </div>
        <div className="mt-3 grid grid-cols-3 gap-1.5">
          {PREVIEW_SLOTS.map((s) => (
            <div
              key={s.time}
              className={
                s.state === "selected"
                  ? "rounded-lg bg-primary py-2 text-center text-xs font-semibold text-primary-foreground tabular"
                  : s.state === "taken"
                    ? "rounded-lg border border-border/60 py-2 text-center text-xs text-muted-foreground line-through opacity-60 tabular"
                    : "rounded-lg border border-border/70 bg-background py-2 text-center text-xs font-medium tabular"
              }
            >
              {s.time}
            </div>
          ))}
        </div>
        <div className="mt-4 flex items-center justify-between gap-3 border-t border-dashed border-border pt-4">
          <div className="text-xs text-muted-foreground">
            Your booking
            <div className="text-sm font-semibold text-foreground tabular">Thu 8 · 10:30 AM</div>
          </div>
          <div className="rounded-[10px] bg-primary px-4 py-2 text-sm font-semibold text-primary-foreground">Confirm</div>
        </div>
      </div>
    </div>
  );
}

export default function Home() {
  return (
    <main className="flex min-h-screen flex-col">
      <header className="mx-auto flex h-16 w-full max-w-6xl items-center justify-between px-4 sm:px-6">
        <Logo />
        <ThemeToggle />
      </header>

      <section className="mx-auto grid w-full max-w-6xl flex-1 items-center gap-12 px-4 py-10 sm:px-6 md:py-16 lg:grid-cols-[1.05fr_1fr]">
        <div className="space-y-6 animate-in fade-in-0 slide-in-from-bottom-2 duration-500">
          <span className="inline-flex items-center gap-2 rounded-full border border-border/70 bg-card px-3 py-1 text-xs font-semibold text-muted-foreground">
            <span className="h-1.5 w-1.5 rounded-full bg-primary" aria-hidden="true" />
            Online booking for local businesses
          </span>
          <h1 className="text-4xl font-extrabold leading-[1.08] tracking-tight sm:text-5xl">
            Welcome to BookingHub
          </h1>
          <p className="max-w-xl text-base text-muted-foreground sm:text-lg">
            The simple way to book appointments with the places you love, and for businesses to fill their
            calendar without the phone ringing all day.
          </p>
          <div className="flex flex-wrap gap-3">
            <Link
              href="/register"
              className="inline-flex h-12 items-center gap-2 rounded-[10px] bg-primary px-6 text-base font-semibold text-primary-foreground shadow-sm transition-colors hover:bg-primary/90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 ring-offset-background"
            >
              Register
              <ArrowRight className="h-4 w-4" aria-hidden="true" />
            </Link>
            <Link
              href="/login"
              className="inline-flex h-12 items-center rounded-[10px] border border-input bg-card px-6 text-base font-semibold transition-colors hover:bg-secondary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 ring-offset-background"
            >
              Login
            </Link>
          </div>
        </div>
        <BookingPreview />
      </section>

      <section className="border-t border-border/70 bg-card/50">
        <div className="mx-auto grid max-w-6xl gap-6 px-4 py-12 sm:px-6 md:grid-cols-3">
          {FEATURES.map(({ icon: Icon, title, text }) => (
            <div key={title} className="flex gap-4">
              <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-accent text-accent-foreground">
                <Icon className="h-5 w-5" aria-hidden="true" />
              </span>
              <div>
                <h2 className="font-semibold">{title}</h2>
                <p className="mt-1 text-sm text-muted-foreground">{text}</p>
              </div>
            </div>
          ))}
        </div>
      </section>

      <footer className="mx-auto w-full max-w-6xl px-4 py-6 text-xs text-muted-foreground sm:px-6">
        © {new Date().getFullYear()} BookingHub
      </footer>
    </main>
  );
}
