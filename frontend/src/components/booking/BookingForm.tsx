"use client";

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { ErrorNotice } from "@/components/ui/error-notice";
import { SlotPicker } from "@/components/booking/SlotPicker";
import { useAvailableSlots } from "@/hooks/queries/useAvailability";
import { useCreateReservation } from "@/hooks/queries/useReservations";
import { toDateInputValue } from "@/lib/dateTime";
import { Service } from "@/types";

const REDIRECT_DELAY_MS = 2000;

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
    <div className="max-w-2xl">
      <h1 className="text-2xl font-bold mb-6">Book {service.name}</h1>
      <Card>
        <CardHeader>
          <CardTitle>{service.name}</CardTitle>
          <CardDescription>
            ${service.price} - {service.durationMinutes} minutes
          </CardDescription>
          <p className="text-sm text-muted-foreground mt-2">
            {service.companyDescription || service.companyName}
          </p>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-6">
            {create.isSuccess && (
              <div className="p-3 text-sm text-green-600 bg-green-50 rounded-md">
                Reservation created successfully! Redirecting to dashboard...
              </div>
            )}
            <ErrorNotice error={create.error} fallback="Failed to create reservation" />

            <div className="space-y-2">
              <Label htmlFor="date">Select Date</Label>
              <Input id="date" type="date" min={today} value={date} onChange={handleDateChange} required />
            </div>

            {date && (
              <>
                <ErrorNotice error={slots.error} fallback="Failed to load available slots" />
                <SlotPicker
                  slots={slots.data ?? []}
                  loading={slots.isLoading}
                  selectedStart={selectedStart}
                  onSelect={(slot) => setSelectedStart(slot.startTime)}
                />
              </>
            )}

            {selectedStart && (
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
            )}

            <div className="flex gap-4">
              <Button type="submit" disabled={create.isPending || create.isSuccess || !selectedStart}>
                {create.isPending ? "Booking..." : "Confirm Booking"}
              </Button>
              <Button type="button" variant="outline" onClick={() => router.back()}>
                Cancel
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
