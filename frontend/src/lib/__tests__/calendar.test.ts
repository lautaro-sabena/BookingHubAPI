import { describe, it, expect } from "vitest";
import {
  buildMonthGrid,
  dayKey,
  getDayCellClasses,
  getStatusClasses,
  groupReservationsByDay,
  shiftMonth,
} from "../calendar";
import { Reservation } from "@/types";

function reservation(id: string, startTime: string): Reservation {
  return {
    id,
    serviceId: "s1",
    customerId: "c1",
    customerEmail: "c@x.com",
    companyId: "co1",
    serviceName: "Cut",
    serviceDuration: 30,
    startTime,
    endTime: startTime,
    status: "Pending",
    createdAt: "2030-01-01T00:00:00Z",
  };
}

describe("buildMonthGrid", () => {
  // January 2030 starts on a Tuesday and has 31 days.
  const january = new Date(2030, 0, 15);
  const today = new Date(2030, 0, 20, 15, 45);

  it("always has 6 full weeks starting on Sunday", () => {
    const grid = buildMonthGrid(january, today);
    expect(grid).toHaveLength(42);
    expect(grid[0].date.getDay()).toBe(0);
  });

  it("pads with the tail of the previous month and the head of the next one", () => {
    const grid = buildMonthGrid(january, today);
    expect(grid.slice(0, 2).map((d) => dayKey(d.date))).toEqual(["2029-12-30", "2029-12-31"]);
    expect(dayKey(grid[2].date)).toBe("2030-01-01");
    expect(dayKey(grid[32].date)).toBe("2030-01-31");
    expect(dayKey(grid[33].date)).toBe("2030-02-01");
    expect(dayKey(grid[41].date)).toBe("2030-02-09");
  });

  it("flags only the days of the requested month as current", () => {
    const grid = buildMonthGrid(january, today);
    expect(grid.filter((d) => d.isCurrentMonth)).toHaveLength(31);
    expect(grid[1].isCurrentMonth).toBe(false);
    expect(grid[2].isCurrentMonth).toBe(true);
  });

  it("marks today once, ignoring the time of day", () => {
    const grid = buildMonthGrid(january, today);
    const marked = grid.filter((d) => d.isToday);
    expect(marked).toHaveLength(1);
    expect(dayKey(marked[0].date)).toBe("2030-01-20");
  });

  it("does not mark today when it only shows up as a padding day of another month", () => {
    const grid = buildMonthGrid(new Date(2030, 1, 1), new Date(2030, 0, 31));
    expect(grid.some((d) => d.isToday)).toBe(false);
  });

  it("handles a month that starts on Sunday (no leading days)", () => {
    // September 2030 starts on a Sunday.
    const grid = buildMonthGrid(new Date(2030, 8, 1), today);
    expect(dayKey(grid[0].date)).toBe("2030-09-01");
    expect(grid[0].isCurrentMonth).toBe(true);
  });
});

describe("shiftMonth", () => {
  it("moves to the first day of the neighbouring month, across year boundaries", () => {
    expect(dayKey(shiftMonth(new Date(2030, 0, 31), -1))).toBe("2029-12-01");
    expect(dayKey(shiftMonth(new Date(2030, 11, 15), 1))).toBe("2031-01-01");
    expect(dayKey(shiftMonth(new Date(2030, 0, 31), 1))).toBe("2030-02-01");
  });
});

describe("groupReservationsByDay", () => {
  it("groups by the company-local day written in the timestamp, whatever its offset", () => {
    const grouped = groupReservationsByDay([
      reservation("a", "2030-01-07T09:00:00-03:00"),
      reservation("b", "2030-01-07T23:30:00+09:00"),
      reservation("c", "2030-01-08T00:15:00-03:00"),
    ]);

    expect(grouped.get("2030-01-07")?.map((r) => r.id)).toEqual(["a", "b"]);
    expect(grouped.get("2030-01-08")?.map((r) => r.id)).toEqual(["c"]);
  });

  it("keeps the local day for a late-evening slot even though its UTC instant is the next day", () => {
    // 22:00 at -03:00 is 01:00 UTC on the 8th, but the company sees it on the 7th.
    const grouped = groupReservationsByDay([reservation("late", "2030-01-07T22:00:00-03:00")]);
    expect([...grouped.keys()]).toEqual(["2030-01-07"]);
  });

  it("separates reservations across month and year boundaries", () => {
    const grouped = groupReservationsByDay([
      reservation("dec", "2029-12-31T23:00:00+00:00"),
      reservation("jan", "2030-01-01T00:00:00+00:00"),
    ]);
    expect([...grouped.keys()].sort()).toEqual(["2029-12-31", "2030-01-01"]);
  });

  it("keeps the API order inside a day and returns nothing for an empty list", () => {
    const grouped = groupReservationsByDay([
      reservation("2", "2030-01-07T15:00:00Z"),
      reservation("1", "2030-01-07T09:00:00Z"),
    ]);
    expect(grouped.get("2030-01-07")?.map((r) => r.id)).toEqual(["2", "1"]);
    expect(groupReservationsByDay([]).size).toBe(0);
  });
});

describe("status and cell styles", () => {
  it("gives every status its own classes per palette", () => {
    expect(getStatusClasses("Confirmed")).toBe("bg-green-100 text-green-800");
    expect(getStatusClasses("Completed")).toBe("bg-blue-100 text-blue-800");
    expect(getStatusClasses("Pending", "themed")).toBe("bg-yellow-500/20 text-yellow-700 dark:text-yellow-300");
    expect(getStatusClasses("Cancelled", "themed")).toBe("bg-red-500/20 text-red-700 dark:text-red-300");
  });

  it("falls back to a neutral style for an unknown status", () => {
    expect(getStatusClasses("Whatever")).toBe("bg-gray-100 text-gray-800");
    expect(getStatusClasses("Whatever", "themed")).toBe("bg-muted text-muted-foreground");
  });

  it("styles day cells by month membership and today", () => {
    const day = { date: new Date(2030, 0, 1), isCurrentMonth: true, isToday: false };
    expect(getDayCellClasses(day, "light")).toBe("bg-white");
    expect(getDayCellClasses({ ...day, isCurrentMonth: false }, "light")).toBe("bg-gray-50");
    expect(getDayCellClasses({ ...day, isToday: true }, "light")).toBe("bg-white border-blue-500 border-2");
    expect(getDayCellClasses({ ...day, isToday: true }, "themed")).toBe("bg-card text-foreground border-primary border-2");
  });
});
