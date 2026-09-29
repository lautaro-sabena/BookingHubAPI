import { describe, it, expect } from "vitest";
import { toCompanyLocalDate, toDateInputValue } from "../dateTime";

describe("toCompanyLocalDate", () => {
  it("reads the wall clock written in the string, ignoring its offset", () => {
    const date = toCompanyLocalDate("2030-01-07T09:00:00-03:00");
    expect([date.getFullYear(), date.getMonth(), date.getDate()]).toEqual([2030, 0, 7]);
    expect([date.getHours(), date.getMinutes()]).toEqual([9, 0]);
  });

  it("shows the same wall clock for the same slot whatever the offset is", () => {
    const utc = toCompanyLocalDate("2030-07-01T14:30:00+00:00");
    const madrid = toCompanyLocalDate("2030-07-01T14:30:00+02:00");
    expect(utc.getTime()).toBe(madrid.getTime());
    expect([utc.getHours(), utc.getMinutes()]).toEqual([14, 30]);
  });

  it("accepts Z, fractional seconds and a missing seconds part", () => {
    expect(toCompanyLocalDate("2030-01-07T09:15:00Z").getMinutes()).toBe(15);
    expect(toCompanyLocalDate("2030-01-07T09:15:00.000+01:00").getHours()).toBe(9);
    expect(toCompanyLocalDate("2030-01-07T09:15-03:00").getHours()).toBe(9);
  });

  it("keeps the calendar day of a late-evening slot", () => {
    const date = toCompanyLocalDate("2030-01-07T23:30:00-03:00");
    expect(date.getDate()).toBe(7);
    expect(date.getHours()).toBe(23);
  });

  it("falls back to the regular Date parser for other formats", () => {
    expect(toCompanyLocalDate("2030-01-07T12:00:00").getTime()).toBe(new Date("2030-01-07T12:00:00").getTime());
    expect(Number.isNaN(toCompanyLocalDate("not a date").getTime())).toBe(true);
  });
});

describe("toDateInputValue", () => {
  it("formats the local calendar day, zero padded", () => {
    expect(toDateInputValue(new Date(2030, 0, 7, 0, 0, 0))).toBe("2030-01-07");
    expect(toDateInputValue(new Date(2030, 11, 31, 23, 59, 0))).toBe("2030-12-31");
  });
});
