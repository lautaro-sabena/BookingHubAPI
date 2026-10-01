import { WorkingHoursResponse } from "@/types";

export const DAYS_OF_WEEK = [
  { value: 0, label: "Sunday" },
  { value: 1, label: "Monday" },
  { value: 2, label: "Tuesday" },
  { value: 3, label: "Wednesday" },
  { value: 4, label: "Thursday" },
  { value: 5, label: "Friday" },
  { value: 6, label: "Saturday" },
];

export const DEFAULT_START_TIME = "09:00";
export const DEFAULT_END_TIME = "17:00";

export interface DayAvailability {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  isActive: boolean;
}

/** A closed day with the default window: what the API assumes for a day without a stored row. */
export function defaultWeek(): DayAvailability[] {
  return DAYS_OF_WEEK.map((d) => ({
    dayOfWeek: d.value,
    startTime: DEFAULT_START_TIME,
    endTime: DEFAULT_END_TIME,
    isActive: false,
  }));
}

/** One entry per weekday, in week order, whatever the API returned (missing days fall back to the default). */
export function normalizeWorkingHours(response: WorkingHoursResponse[]): DayAvailability[] {
  return defaultWeek().map((fallback) => {
    const stored = response.find((wh) => Number(wh.dayOfWeek) === fallback.dayOfWeek);
    if (!stored) return fallback;
    return {
      dayOfWeek: fallback.dayOfWeek,
      startTime: typeof stored.startTime === "string" && stored.startTime ? stored.startTime : fallback.startTime,
      endTime: typeof stored.endTime === "string" && stored.endTime ? stored.endTime : fallback.endTime,
      isActive: stored.isActive,
    };
  });
}

/**
 * The PUT /workinghours body: every weekday, inactive ones too. The API stores closed days with their times, so a
 * day the owner switched off keeps its custom window for when it is switched on again. A cleared time input
 * ("") would not bind to a TimeSpan, so it falls back to the default.
 */
export function toWorkingHoursPayload(days: DayAvailability[]): DayAvailability[] {
  return days.map((d) => ({
    dayOfWeek: d.dayOfWeek,
    startTime: d.startTime || DEFAULT_START_TIME,
    endTime: d.endTime || DEFAULT_END_TIME,
    isActive: d.isActive,
  }));
}
