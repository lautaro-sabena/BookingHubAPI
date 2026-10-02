"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ErrorNotice } from "@/components/ui/error-notice";
import { SuccessNotice } from "@/components/ui/notice";
import { PageHeader } from "@/components/ui/page-header";
import { cn } from "@/lib/utils";
import type { useSaveWorkingHours } from "@/hooks/queries/useWorkingHours";
import { DAYS_OF_WEEK, DayAvailability } from "@/lib/workingHours";

/**
 * Weekly opening hours editor. It takes the saved week once: key it by whatever should reset it. The save mutation
 * is owned by the caller so its success and error state survive a remount (the refetch after a save changes the key).
 */
export function WorkingHoursForm({
  initial,
  save,
}: {
  initial: DayAvailability[];
  save: ReturnType<typeof useSaveWorkingHours>;
}) {
  const [days, setDays] = useState(initial);

  const updateDay = (dayOfWeek: number, changes: Partial<DayAvailability>) => {
    setDays((prev) => prev.map((d) => (d.dayOfWeek === dayOfWeek ? { ...d, ...changes } : d)));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    save.mutate(days);
  };

  const openDays = days.filter((d) => d.isActive).length;

  return (
    <div className="max-w-3xl space-y-6">
      <PageHeader
        title="Working Hours"
        description="Customers can only book inside these hours, in your company's time zone."
      />
      <Card>
        <CardHeader className="flex-row items-center justify-between gap-4 space-y-0">
          <CardTitle>Configure Your Availability</CardTitle>
          <span className="rounded-full bg-accent px-2.5 py-1 text-xs font-semibold text-accent-foreground tabular">
            {openDays} {openDays === 1 ? "day" : "days"} open
          </span>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-5">
            {save.isSuccess && <SuccessNotice>Availability saved successfully!</SuccessNotice>}
            <ErrorNotice error={save.error} fallback="Failed to save availability" />
            <div className="divide-y divide-border/70 overflow-hidden rounded-xl border border-border/70">
              {DAYS_OF_WEEK.map((day) => {
                const current = days.find((d) => d.dayOfWeek === day.value);
                const isOpen = current?.isActive || false;
                return (
                  <div
                    key={day.value}
                    className={cn(
                      "flex flex-wrap items-center gap-x-4 gap-y-3 px-4 py-3 transition-colors",
                      isOpen ? "bg-card" : "bg-muted/40"
                    )}
                  >
                    <label className="flex w-40 cursor-pointer items-center gap-3">
                      {/* Native checkbox styled as a switch: same role, label and keyboard behavior. */}
                      <input
                        type="checkbox"
                        aria-label={`${day.label} open`}
                        checked={isOpen}
                        onChange={(e) => updateDay(day.value, { isActive: e.target.checked })}
                        className={cn(
                          "relative h-6 w-11 shrink-0 cursor-pointer appearance-none rounded-full bg-input transition-colors",
                          "before:absolute before:left-0.5 before:top-0.5 before:h-5 before:w-5 before:rounded-full before:bg-card before:shadow-sm before:transition-transform before:content-['']",
                          "checked:bg-primary checked:before:translate-x-5",
                          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 ring-offset-background"
                        )}
                      />
                      <span className={cn("font-semibold", !isOpen && "text-muted-foreground")}>{day.label}</span>
                    </label>
                    {isOpen ? null : (
                      <span className="text-xs font-medium uppercase tracking-[0.06em] text-muted-foreground sm:hidden">Closed</span>
                    )}
                    <div className="flex flex-1 items-center gap-2 sm:justify-end">
                      <Input
                        type="time"
                        aria-label={`${day.label} start`}
                        value={current?.startTime || "09:00"}
                        onChange={(e) => updateDay(day.value, { startTime: e.target.value })}
                        className="w-32"
                        disabled={!current?.isActive}
                      />
                      <span className="text-sm text-muted-foreground">to</span>
                      <Input
                        type="time"
                        aria-label={`${day.label} end`}
                        value={current?.endTime || "17:00"}
                        onChange={(e) => updateDay(day.value, { endTime: e.target.value })}
                        className="w-32"
                        disabled={!current?.isActive}
                      />
                    </div>
                  </div>
                );
              })}
            </div>
            <div className="flex justify-end">
              <Button type="submit" size="lg" disabled={save.isPending}>
                {save.isPending ? "Saving..." : "Save Availability"}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
