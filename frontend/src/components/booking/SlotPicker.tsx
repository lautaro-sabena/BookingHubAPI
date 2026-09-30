"use client";

import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { formatCompanyTime } from "@/lib/dateTime";
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
    <div className="space-y-2">
      <Label>Available Time Slots</Label>
      {loading ? (
        <p className="text-sm text-muted-foreground">Loading available slots...</p>
      ) : slots.length > 0 ? (
        <div className="grid grid-cols-3 sm:grid-cols-4 gap-2">
          {slots.map((slot) => (
            <Button
              key={slot.startTime}
              type="button"
              variant={selectedStart === slot.startTime ? "default" : "outline"}
              size="sm"
              onClick={() => onSelect(slot)}
              className="text-sm"
            >
              {formatCompanyTime(slot.startTime)}
            </Button>
          ))}
        </div>
      ) : (
        <p className="text-sm text-muted-foreground">
          No available slots for this date. Please select another date.
        </p>
      )}
    </div>
  );
}
