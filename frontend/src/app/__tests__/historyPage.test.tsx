// @vitest-environment jsdom
import { describe, it, expect, vi, afterEach } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import { sortByInstantDescending } from "@/lib/reservations";
import { Reservation } from "@/types";

const { reservationsState } = vi.hoisted(() => ({
  reservationsState: { data: [] as unknown[], isLoading: false, error: null as unknown },
}));
vi.mock("@/hooks/queries/useReservations", () => ({ useReservations: () => reservationsState }));
vi.mock("@/hooks/useRequireRole", () => ({
  useRequireRole: () => ({ user: { id: "u1", role: "Customer" }, authLoading: false, allowed: true }),
}));

import CustomerHistoryPage from "../dashboard/customer/history/page";

function reservation(id: string, serviceName: string, startTime: string): Reservation {
  return {
    id,
    serviceId: "s",
    customerId: "c",
    customerEmail: "c@x.com",
    companyId: "co",
    serviceName,
    serviceDuration: 30,
    startTime,
    endTime: startTime,
    status: "Confirmed",
    createdAt: "2030-01-01T00:00:00Z",
  };
}

// The wall clocks (Auckland 01-08 02:00, LA 01-07 08:00, UTC 01-07 20:00) rank Auckland latest, but the instants
// (13:00Z, 16:00Z, 20:00Z on 01-07) rank it earliest.
const auckland = reservation("akl", "Auckland cut", "2030-01-08T02:00:00+13:00"); // 2030-01-07T13:00Z
const losAngeles = reservation("lax", "LA cut", "2030-01-07T08:00:00-08:00"); //     2030-01-07T16:00Z
const utc = reservation("utc", "UTC cut", "2030-01-07T20:00:00+00:00"); //           2030-01-07T20:00Z

describe("sortByInstantDescending", () => {
  it("orders by the real instant, not by the company wall clock or the string", () => {
    // Wall clocks (akl 01-08 02:00, lax 01-07 08:00, utc 01-07 20:00) would put Auckland first.
    const sorted = sortByInstantDescending([auckland, losAngeles, utc]);
    expect(sorted.map((r) => r.id)).toEqual(["utc", "lax", "akl"]);
  });

  it("does not mutate its input", () => {
    const input = [auckland, utc];
    sortByInstantDescending(input);
    expect(input.map((r) => r.id)).toEqual(["akl", "utc"]);
  });
});

describe("CustomerHistoryPage", () => {
  afterEach(cleanup);

  it("lists reservations of companies in different offsets most recent instant first", () => {
    reservationsState.data = [auckland, losAngeles, utc];

    render(<CustomerHistoryPage />);

    const titles = screen.getAllByRole("heading", { level: 3 }).map((h) => h.textContent);
    expect(titles).toEqual(["UTC cut", "LA cut", "Auckland cut"]);
  });

  it("shows the empty state without reservations", () => {
    reservationsState.data = [];
    render(<CustomerHistoryPage />);
    expect(screen.getByText(/No reservations yet/)).toBeTruthy();
  });
});
