"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ErrorNotice } from "@/components/ui/error-notice";
import { useSaveWorkingHours } from "@/hooks/queries/useWorkingHours";
import { DAYS_OF_WEEK, DayAvailability } from "@/lib/workingHours";

/** Weekly opening hours editor. It takes the saved week once: key it by whatever should reset it. */
export function WorkingHoursForm({ initial }: { initial: DayAvailability[] }) {
  const [days, setDays] = useState(initial);
  const save = useSaveWorkingHours();

  const updateDay = (dayOfWeek: number, changes: Partial<DayAvailability>) => {
    setDays((prev) => prev.map((d) => (d.dayOfWeek === dayOfWeek ? { ...d, ...changes } : d)));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    save.mutate(days);
  };

  return (
    <div className="max-w-2xl">
      <h1 className="text-2xl font-bold mb-6">Working Hours</h1>
      <Card>
        <CardHeader>
          <CardTitle>Configure Your Availability</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            {save.isSuccess && (
              <div className="p-3 text-sm text-green-600 bg-green-50 rounded-md">
                Availability saved successfully!
              </div>
            )}
            <ErrorNotice error={save.error} fallback="Failed to save availability" />
            {DAYS_OF_WEEK.map((day) => {
              const current = days.find((d) => d.dayOfWeek === day.value);
              return (
                <div key={day.value} className="flex items-center gap-4">
                  <div className="w-8">
                    <input
                      type="checkbox"
                      aria-label={`${day.label} open`}
                      checked={current?.isActive || false}
                      onChange={(e) => updateDay(day.value, { isActive: e.target.checked })}
                      className="w-4 h-4"
                    />
                  </div>
                  <div className="w-24 font-medium">{day.label}</div>
                  <Input
                    type="time"
                    aria-label={`${day.label} start`}
                    value={current?.startTime || "09:00"}
                    onChange={(e) => updateDay(day.value, { startTime: e.target.value })}
                    className="w-32"
                    disabled={!current?.isActive}
                  />
                  <span>to</span>
                  <Input
                    type="time"
                    aria-label={`${day.label} end`}
                    value={current?.endTime || "17:00"}
                    onChange={(e) => updateDay(day.value, { endTime: e.target.value })}
                    className="w-32"
                    disabled={!current?.isActive}
                  />
                </div>
              );
            })}
            <Button type="submit" disabled={save.isPending}>
              {save.isPending ? "Saving..." : "Save Availability"}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
