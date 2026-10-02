// @vitest-environment jsdom
import { describe, it, expect, afterEach } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import { StatusBadge } from "../status-badge";
import { getStatusClasses } from "@/lib/calendar";

function badge(status: string) {
  render(<StatusBadge status={status} />);
  return screen.getByText(status);
}

describe("StatusBadge", () => {
  afterEach(cleanup);

  it.each([
    ["Pending", "bg-warning-soft", "text-warning"],
    ["Confirmed", "bg-success-soft", "text-success"],
    ["Completed", "bg-accent", "text-accent-foreground"],
    ["Cancelled", "bg-destructive-soft", "text-destructive"],
  ])("uses the theme tokens for the known status %s", (status, bg, text) => {
    const el = badge(status);
    expect(el.classList.contains(bg)).toBe(true);
    expect(el.classList.contains(text)).toBe(true);
  });

  it("matches known statuses case-insensitively", () => {
    expect(badge("PENDING").classList.contains("bg-warning-soft")).toBe(true);
  });

  it("falls back to the shared calendar classes for an unknown status", () => {
    const el = badge("Rescheduled");
    for (const cls of getStatusClasses("Rescheduled").split(" ")) {
      expect(el.classList.contains(cls)).toBe(true);
    }
    expect(el.classList.contains("bg-warning-soft")).toBe(false);
  });

  it("shows the status text and merges an extra className", () => {
    render(<StatusBadge status="Pending" className="ml-2" />);
    const el = screen.getByText("Pending");
    expect(el.classList.contains("ml-2")).toBe(true);
  });
});
