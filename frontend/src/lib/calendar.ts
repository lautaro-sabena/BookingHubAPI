import { toCompanyLocalDate, toDateInputValue } from "@/lib/dateTime";
import { Reservation, ReservationStatus } from "@/types";

export const MONTH_NAMES = [
  "January", "February", "March", "April", "May", "June",
  "July", "August", "September", "October", "November", "December",
];

export const DAY_NAMES = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

export interface CalendarDay {
  date: Date;
  isCurrentMonth: boolean;
  isToday: boolean;
}

const GRID_CELLS = 6 * 7;

/**
 * The month as a fixed 6-week grid starting on Sunday: leading days of the previous month, the month itself and
 * trailing days of the next one. `today` is injectable so the result is deterministic in tests.
 */
export function buildMonthGrid(month: Date, today: Date = new Date()): CalendarDay[] {
  const year = month.getFullYear();
  const monthIndex = month.getMonth();
  const startOfToday = new Date(today.getFullYear(), today.getMonth(), today.getDate()).getTime();
  const leading = new Date(year, monthIndex, 1).getDay();

  return Array.from({ length: GRID_CELLS }, (_, cell) => {
    // Day 1 of the month sits at index `leading`; Date normalises days outside 1..N into the adjacent months.
    const date = new Date(year, monthIndex, cell - leading + 1);
    const isCurrentMonth = date.getMonth() === monthIndex;
    return { date, isCurrentMonth, isToday: isCurrentMonth && date.getTime() === startOfToday };
  });
}

/** First day of the month `delta` months away from `date` (negative = earlier). */
export function shiftMonth(date: Date, delta: number): Date {
  return new Date(date.getFullYear(), date.getMonth() + delta, 1);
}

/** The calendar-day key ("YYYY-MM-DD") a cell or a reservation belongs to. */
export function dayKey(date: Date): string {
  return toDateInputValue(date);
}

/**
 * Reservations grouped by the company-local day they start on (the wall clock written in the API timestamp, not the
 * viewer's zone), keeping the API order inside each day. Reservations near midnight therefore land on the day the
 * company sees, across month boundaries too.
 */
export function groupReservationsByDay(reservations: Reservation[]): Map<string, Reservation[]> {
  const byDay = new Map<string, Reservation[]>();
  for (const reservation of reservations) {
    const key = dayKey(toCompanyLocalDate(reservation.startTime));
    const group = byDay.get(key);
    if (group) group.push(reservation);
    else byDay.set(key, [reservation]);
  }
  return byDay;
}

/** "light" = fixed light colours (customer pages); "themed" = colours that follow the light/dark theme. */
export type CalendarPalette = "light" | "themed";

interface PaletteClasses {
  currentMonth: string;
  otherMonth: string;
  today: string;
  status: Record<ReservationStatus, string>;
  /** Unknown status coming from the API. */
  fallbackStatus: string;
}

const PALETTES: Record<CalendarPalette, PaletteClasses> = {
  light: {
    currentMonth: "bg-white",
    otherMonth: "bg-gray-50",
    today: "border-blue-500 border-2",
    status: {
      Pending: "bg-yellow-100 text-yellow-800",
      Confirmed: "bg-green-100 text-green-800",
      Cancelled: "bg-red-100 text-red-800",
      Completed: "bg-blue-100 text-blue-800",
    },
    fallbackStatus: "bg-gray-100 text-gray-800",
  },
  themed: {
    currentMonth: "bg-card text-foreground",
    otherMonth: "bg-muted text-muted-foreground",
    today: "border-primary border-2",
    status: {
      Pending: "bg-yellow-500/20 text-yellow-700 dark:text-yellow-300",
      Confirmed: "bg-green-500/20 text-green-700 dark:text-green-300",
      Cancelled: "bg-red-500/20 text-red-700 dark:text-red-300",
      Completed: "bg-muted text-muted-foreground",
    },
    fallbackStatus: "bg-muted text-muted-foreground",
  },
};

/** Tailwind classes for a reservation status chip/badge. */
export function getStatusClasses(status: string, palette: CalendarPalette = "light"): string {
  const { status: byStatus, fallbackStatus } = PALETTES[palette];
  return (byStatus as Record<string, string>)[status] ?? fallbackStatus;
}

/** Tailwind classes for a day cell. */
export function getDayCellClasses(day: CalendarDay, palette: CalendarPalette = "light"): string {
  const classes = PALETTES[palette];
  return `${day.isCurrentMonth ? classes.currentMonth : classes.otherMonth}${day.isToday ? ` ${classes.today}` : ""}`;
}
