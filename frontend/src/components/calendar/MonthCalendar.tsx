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
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle>{MONTH_NAMES[currentDate.getMonth()]} {currentDate.getFullYear()}</CardTitle>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" onClick={() => setCurrentDate(shiftMonth(currentDate, -1))}>
              Previous
            </Button>
            <Button variant="outline" size="sm" onClick={() => setCurrentDate(shiftMonth(currentDate, 1))}>
              Next
            </Button>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-7 gap-1">
          {DAY_NAMES.map((day) => (
            <div key={day} className="text-center text-sm font-medium text-muted-foreground py-2">
              {day}
            </div>
          ))}
          {days.map((day) => {
            const dayReservations = byDay.get(dayKey(day.date)) ?? [];
            return (
              <div key={day.date.getTime()} className={`min-h-[80px] border p-1 ${getDayCellClasses(day, palette)}`}>
                <div className="text-sm font-medium">{day.date.getDate()}</div>
                <div className="space-y-1">
                  {dayReservations.slice(0, VISIBLE_PER_DAY).map((res) => (
                    <div
                      key={res.id}
                      onClick={() => onSelect(res)}
                      className={`text-xs p-1 rounded cursor-pointer truncate ${getStatusClasses(res.status, palette)}`}
                    >
                      {formatCompanyTime(res.startTime)} - {res.serviceName}
                    </div>
                  ))}
                  {dayReservations.length > VISIBLE_PER_DAY && (
                    <div className="text-xs text-muted-foreground">
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
