"use client";

import { CalendarX2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { formatCompanyTime } from "@/lib/dateTime";
import { cn } from "@/lib/utils";
import { AvailableSlot } from "@/types";

interface SlotPickerProps {
  slots: AvailableSlot[];
  loading: boolean;
  selectedStart: string | null;
  onSelect: (slot: AvailableSlot) => void;
}

/** The free slots of the chosen day, shown in the company's local time. */
export function SlotPicker({ slots, loading, selectedStart, onSelect }: SlotPickerProps) {
  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <Label>Available Time Slots</Label>
        {!loading && slots.length > 0 && (
          <span className="text-xs text-muted-foreground tabular">{slots.length} available</span>
        )}
      </div>
      {loading ? (
        <div className="space-y-3">
          <p className="text-sm text-muted-foreground">Loading available slots...</p>
          <div aria-hidden="true" className="grid grid-cols-3 gap-2 sm:grid-cols-4">
            {Array.from({ length: 8 }, (_, i) => (
              <Skeleton key={i} className="h-10" />
            ))}
          </div>
        </div>
      ) : slots.length > 0 ? (
        <div className="grid grid-cols-3 gap-2 sm:grid-cols-4">
          {slots.map((slot) => {
            const selected = selectedStart === slot.startTime;
            return (
              <Button
                key={slot.startTime}
                type="button"
                variant={selected ? "default" : "outline"}
                size="sm"
                aria-pressed={selected}
                onClick={() => onSelect(slot)}
                className={cn("h-10 text-sm tabular", selected ? "shadow-md ring-2 ring-primary/25" : "font-medium")}
              >
                {formatCompanyTime(slot.startTime)}
              </Button>
            );
          })}
        </div>
      ) : (
        <div className="flex items-start gap-3 rounded-lg border border-dashed border-border bg-muted/40 px-4 py-4">
          <CalendarX2 className="mt-0.5 h-5 w-5 shrink-0 text-muted-foreground" aria-hidden="true" />
          <p className="text-sm text-muted-foreground">
            No available slots for this date. Please select another date.
          </p>
        </div>
      )}
    </div>
  );
}
