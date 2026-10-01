import { describe, it, expect } from "vitest";
import { defaultWeek, normalizeWorkingHours, toWorkingHoursPayload } from "../workingHours";

describe("normalizeWorkingHours", () => {
  it("returns the seven days in week order, filling missing ones with a closed default window", () => {
    const week = normalizeWorkingHours([
      { dayOfWeek: 3, startTime: "10:00:00", endTime: "14:00:00", isActive: true },
    ]);

    expect(week.map((d) => d.dayOfWeek)).toEqual([0, 1, 2, 3, 4, 5, 6]);
    expect(week[3]).toEqual({ dayOfWeek: 3, startTime: "10:00:00", endTime: "14:00:00", isActive: true });
    expect(week[0]).toEqual({ dayOfWeek: 0, startTime: "09:00", endTime: "17:00", isActive: false });
  });

  it("keeps the custom times of an inactive day", () => {
    const week = normalizeWorkingHours([
      { dayOfWeek: 1, startTime: "08:30:00", endTime: "12:00:00", isActive: false },
    ]);
    expect(week[1]).toEqual({ dayOfWeek: 1, startTime: "08:30:00", endTime: "12:00:00", isActive: false });
  });
});

describe("toWorkingHoursPayload", () => {
  it("sends every weekday, inactive ones included, with their times (PUT /workinghours body)", () => {
    const days = defaultWeek();
    days[1] = { dayOfWeek: 1, startTime: "08:00", endTime: "12:00", isActive: true };
    days[2] = { dayOfWeek: 2, startTime: "10:00", endTime: "13:30", isActive: false };

    const payload = toWorkingHoursPayload(days);

    expect(payload).toHaveLength(7);
    expect(payload[1]).toEqual({ dayOfWeek: 1, startTime: "08:00", endTime: "12:00", isActive: true });
    // The closed day keeps the owner's custom window: the API stores it (WorkingHours.Validate only requires
    // start < end on active days, and every weekday may appear at most once).
    expect(payload[2]).toEqual({ dayOfWeek: 2, startTime: "10:00", endTime: "13:30", isActive: false });
    expect(new Set(payload.map((d) => d.dayOfWeek)).size).toBe(7);
  });

  it("replaces a cleared time input, which would not bind to a TimeSpan, with the default", () => {
    const payload = toWorkingHoursPayload([{ dayOfWeek: 0, startTime: "", endTime: "", isActive: false }]);
    expect(payload).toEqual([{ dayOfWeek: 0, startTime: "09:00", endTime: "17:00", isActive: false }]);
  });
});
