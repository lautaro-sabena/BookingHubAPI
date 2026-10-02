"use client";

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { ErrorNotice } from "@/components/ui/error-notice";
import { SlotPicker } from "@/components/booking/SlotPicker";
import { SuccessNotice } from "@/components/ui/notice";
import { PageHeader } from "@/components/ui/page-header";
import { useAvailableSlots } from "@/hooks/queries/useAvailability";
import { useCreateReservation } from "@/hooks/queries/useReservations";
import { formatCompanyTime, toDateInputValue } from "@/lib/dateTime";
import { cn } from "@/lib/utils";
import { Check, Clock } from "lucide-react";
import { Service } from "@/types";

const REDIRECT_DELAY_MS = 2000;

/**
 * Label for the picked "YYYY-MM-DD" day (e.g. "Thu, Oct 8"). The string names a calendar day of the company, so it is
 * read as a UTC date and formatted in UTC: the browser's own time zone never shifts it.
 */
function formatPickedDay(value: string) {
  const [y, m, d] = value.split("-").map(Number);
  if (!y || !m || !d) return value;
  return new Date(Date.UTC(y, m - 1, d)).toLocaleDateString("en-US", {
    weekday: "short",
    month: "short",
    day: "numeric",
    timeZone: "UTC",
  });
}

/** Pick a day, pick one of its free slots, add notes, book. */
export function BookingForm({ service }: { service: Service }) {
  const router = useRouter();
  // The date input's own value ("YYYY-MM-DD"): it names the company's day and is sent to the API untouched.
  const [date, setDate] = useState("");
  const [selectedStart, setSelectedStart] = useState<string | null>(null);
  const [notes, setNotes] = useState("");
  const slots = useAvailableSlots(service.id, date);
  const create = useCreateReservation();
  // Set synchronously on submit: the mutation reports "pending" a tick later, so a fast double click would slip through.
  const submitted = useRef(false);

  const today = toDateInputValue(new Date());

  useEffect(() => {
    if (!create.isSuccess) return;
    const timer = setTimeout(() => router.push("/dashboard"), REDIRECT_DELAY_MS);
    return () => clearTimeout(timer);
  }, [create.isSuccess, router]);

  const handleDateChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setDate(e.target.value);
    setSelectedStart(null);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedStart || submitted.current) return;
    submitted.current = true;
    // The slot's ISO string goes back exactly as the API produced it (offset included), never rebuilt from a Date.
    create.mutate(
      { serviceId: service.id, startTime: selectedStart, notes: notes || null },
      // A failed booking may be retried; a successful one stays locked until the redirect.
      { onError: () => (submitted.current = false) },
    );
  };

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={service.companyName}
        title={`Book ${service.name}`}
        description="Choose a day and a free time. Times are shown in the business's local time."
      />
      <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.5fr)]">
        <Card className="overflow-hidden lg:sticky lg:top-24">
          <div
            aria-hidden="true"
            className="h-24 bg-gradient-to-br from-accent to-primary/30"
          />
          <CardHeader className="-mt-10 gap-3">
            <span
              aria-hidden="true"
              className="flex h-14 w-14 items-center justify-center rounded-2xl border-4 border-card bg-primary text-xl font-bold text-primary-foreground shadow-card"
            >
              {service.name.charAt(0).toUpperCase()}
            </span>
            <div className="space-y-1">
              <CardTitle className="text-xl">{service.name}</CardTitle>
              <CardDescription className="flex flex-wrap gap-2 pt-1">
                <span className="inline-flex items-center gap-1 rounded-full bg-accent px-2.5 py-1 text-xs font-semibold text-accent-foreground tabular">
                  ${service.price}
                </span>
                <span className="inline-flex items-center gap-1 rounded-full bg-accent px-2.5 py-1 text-xs font-semibold text-accent-foreground tabular">
                  <Clock className="h-3 w-3" aria-hidden="true" />
                  {service.durationMinutes} minutes
                </span>
              </CardDescription>
            </div>
            <p className="text-sm text-muted-foreground">
              {service.companyDescription || service.companyName}
            </p>
          </CardHeader>
        </Card>

        <Card>
          <CardContent className="pt-5 sm:pt-6">
            <form onSubmit={handleSubmit} className="space-y-6">
              {create.isSuccess && (
                <SuccessNotice>Reservation created successfully! Redirecting to dashboard...</SuccessNotice>
              )}
              <ErrorNotice error={create.error} fallback="Failed to create reservation" />

              <Step number={1} done={!!date}>
                <div className="space-y-2">
                  <Label htmlFor="date">Select Date</Label>
                  <Input id="date" type="date" min={today} value={date} onChange={handleDateChange} required className="h-11 sm:max-w-xs" />
                </div>
              </Step>

              <Step number={2} done={!!selectedStart} muted={!date}>
                {date ? (
                  <div className="space-y-3">
                    <ErrorNotice error={slots.error} fallback="Failed to load available slots" />
                    <SlotPicker
                      slots={slots.data ?? []}
                      loading={slots.isLoading}
                      selectedStart={selectedStart}
                      onSelect={(slot) => setSelectedStart(slot.startTime)}
                    />
                  </div>
                ) : (
                  <p className="pt-2 text-sm text-muted-foreground">Pick a date to see the free times.</p>
                )}
              </Step>

              {selectedStart && (
                <Step number={3} last>
                  <div className="space-y-2">
                    <Label htmlFor="notes">Notes (optional)</Label>
                    <Input
                      id="notes"
                      type="text"
                      placeholder="Any special requests..."
                      value={notes}
                      onChange={(e) => setNotes(e.target.value)}
                    />
                  </div>
                </Step>
              )}

              <div className="flex flex-col gap-4 border-t border-dashed border-border pt-5 sm:flex-row sm:items-center sm:justify-between">
                <div className="text-sm">
                  {selectedStart ? (
                    <>
                      <p className="text-muted-foreground">Your booking</p>
                      <p className="font-semibold tabular">
                        {formatPickedDay(date)} · {formatCompanyTime(selectedStart)}
                      </p>
                      <p className="text-xs text-muted-foreground">
                        {service.durationMinutes} min · ${service.price}
                      </p>
                    </>
                  ) : (
                    <p className="text-muted-foreground">Select a time to continue</p>
                  )}
                </div>
                <div className="flex gap-3 sm:flex-row-reverse">
                  <Button type="submit" size="lg" className="flex-1 sm:flex-none" disabled={create.isPending || create.isSuccess || !selectedStart}>
                    {create.isPending ? "Booking..." : "Confirm Booking"}
                  </Button>
                  <Button type="button" size="lg" variant="ghost" onClick={() => router.back()}>
                    Cancel
                  </Button>
                </div>
              </div>
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

/** Numbered step of the booking flow with a connecting line. Purely visual. */
function Step({
  number,
  done,
  muted,
  last,
  children,
}: {
  number: number;
  done?: boolean;
  muted?: boolean;
  last?: boolean;
  children: React.ReactNode;
}) {
  return (
    <div className="relative flex gap-4">
      <div className="flex flex-col items-center">
        <span
          aria-hidden="true"
          className={cn(
            "flex h-7 w-7 shrink-0 items-center justify-center rounded-full text-xs font-bold transition-colors",
            done ? "bg-primary text-primary-foreground" : muted ? "bg-muted text-muted-foreground" : "bg-accent text-accent-foreground"
          )}
        >
          {done ? <Check className="h-3.5 w-3.5" /> : number}
        </span>
        {!last && <span aria-hidden="true" className="mt-2 w-px flex-1 bg-border" />}
      </div>
      <div className={cn("min-w-0 flex-1 pb-1", muted && "opacity-70")}>{children}</div>
    </div>
  );
}
