"use client";

import { useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  CalendarPalette,
  DAY_NAMES,
  MONTH_NAMES,
  buildMonthGrid,
  dayKey,
  getDayCellClasses,
  getStatusClasses,
  groupReservationsByDay,
  shiftMonth,
} from "@/lib/calendar";
import { formatCompanyTime } from "@/lib/dateTime";
import { Reservation } from "@/types";
import { ChevronLeft, ChevronRight } from "lucide-react";

const VISIBLE_PER_DAY = 2;

interface MonthCalendarProps {
  reservations: Reservation[];
  palette: CalendarPalette;
  onSelect: (reservation: Reservation) => void;
}

/** Month grid with the reservations of each company-local day. It owns only which month is displayed. */
export function MonthCalendar({ reservations, palette, onSelect }: MonthCalendarProps) {
  const [currentDate, setCurrentDate] = useState(() => new Date());
  const days = useMemo(() => buildMonthGrid(currentDate), [currentDate]);
  const byDay = useMemo(() => groupReservationsByDay(reservations), [reservations]);

  return (
    <Card className="overflow-hidden">
      <CardHeader className="border-b border-border/70 pb-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <CardTitle className="text-xl tabular">
            {MONTH_NAMES[currentDate.getMonth()]} <span className="text-muted-foreground">{currentDate.getFullYear()}</span>
          </CardTitle>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" onClick={() => setCurrentDate(shiftMonth(currentDate, -1))}>
              <ChevronLeft className="h-4 w-4" aria-hidden="true" />
              Previous
            </Button>
            <Button variant="outline" size="sm" onClick={() => setCurrentDate(shiftMonth(currentDate, 1))}>
              Next
              <ChevronRight className="h-4 w-4" aria-hidden="true" />
            </Button>
          </div>
        </div>
      </CardHeader>
      <CardContent className="overflow-x-auto p-3 pt-3 sm:p-4 sm:pt-4">
        <div className="grid min-w-[640px] grid-cols-7 gap-1.5">
          {DAY_NAMES.map((day) => (
            <div key={day} className="py-2 text-center text-xs font-semibold uppercase tracking-[0.06em] text-muted-foreground">
              {day}
            </div>
          ))}
          {days.map((day) => {
            const dayReservations = byDay.get(dayKey(day.date)) ?? [];
            return (
              <div
                key={day.date.getTime()}
                className={`min-h-[96px] rounded-lg border border-border/70 p-1.5 transition-colors ${getDayCellClasses(day, palette)}`}
              >
                <div className="mb-1 px-1 text-sm font-semibold tabular">{day.date.getDate()}</div>
                <div className="space-y-1">
                  {dayReservations.slice(0, VISIBLE_PER_DAY).map((res) => (
                    <button
                      type="button"
                      key={res.id}
                      onClick={() => onSelect(res)}
                      className={`block w-full truncate rounded-md px-1.5 py-1 text-left text-xs font-medium transition-opacity hover:opacity-80 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring tabular ${getStatusClasses(res.status, palette)}`}
                    >
                      {formatCompanyTime(res.startTime)} - {res.serviceName}
                    </button>
                  ))}
                  {dayReservations.length > VISIBLE_PER_DAY && (
                    <div className="px-1.5 text-xs font-medium text-muted-foreground">
                      +{dayReservations.length - VISIBLE_PER_DAY} more
                    </div>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      </CardContent>
    </Card>
  );
}
